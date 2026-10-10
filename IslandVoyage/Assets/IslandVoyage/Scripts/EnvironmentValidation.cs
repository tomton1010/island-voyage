using System;
using UnityEngine;

namespace IslandVoyage
{
    // Runs only in --self-test, against the generated scene, colliders and imported models.
    public static class EnvironmentValidation
    {
        static void Check(bool value,string label)
        {if(!value)throw new InvalidOperationException("Environment check failed: "+label);}

        public static void Validate(GameBootstrap game)
        {
            Physics.SyncTransforms();
            foreach(var island in WorldLayout.Islands)foreach(float ratio in new[]{.5f,.98f,1.02f,1.3f,1.68f,1.72f})
            for(int sector=0;sector<12;sector++)
            {
                float angle=sector*Mathf.PI/6,radial=ratio*island.Edge(angle);
                Vector3 p=new Vector3(island.center.x+Mathf.Cos(angle)*island.size.x*radial,0,island.center.y+Mathf.Sin(angle)*island.size.y*radial);
                float expected=WorldLayout.GroundHeight(p);
                if(ratio<1.7f)
                {
                    Check(Physics.Raycast(p+Vector3.up*10,Vector3.down,out var hit,20),"Coastal land and submerged shelf have collision geometry");
                    // Scenery can be hit first on land; inspect the terrain collider directly.
                    var terrain=GameObject.Find("Sand island "+Array.IndexOf(WorldLayout.Islands,island)).GetComponent<MeshCollider>();
                    Check(terrain.Raycast(new Ray(p+Vector3.up*10,Vector3.down),out hit,20) && Mathf.Abs(hit.point.y-expected)<.055f,"Terrain sampling agrees with the rendered coast and seabed");
                }
                else Check(Mathf.Abs(expected-WorldLayout.SeabedHeight)<.001f,"Coastal shelf joins the offshore bed continuously");
            }
            float reefClearance=float.MaxValue;int reefs=0;
            var world=GameObject.Find("Tropical archipelago · Blender scenery and simulated ocean").transform;
            foreach(Transform child in world)
            {
                if(child.name.StartsWith("Reef") && child.gameObject.activeSelf)
                {
                    reefs++;Check(Mathf.Abs(child.position.y-WorldLayout.GroundHeight(child.position)-.04f)<.001f,"Coral is anchored to the local seabed");
                    foreach(var renderer in child.GetComponentsInChildren<Renderer>())
                    {
                        reefClearance=Mathf.Min(reefClearance,-renderer.bounds.max.y);
                        var properties=new MaterialPropertyBlock();renderer.GetPropertyBlock(properties);
                        Check(properties.GetFloat("_Aquatic")==1,"Coral uses underwater light absorption");
                    }
                }
                if(child.name=="Seagrass meadows")
                {
                    Vector3[] vertices=child.GetComponent<MeshFilter>().sharedMesh.vertices;
                    // Each pair of crossed triangles has four root vertices and two tips.
                    for(int i=0;i<vertices.Length;i+=6)foreach(int offset in new[]{0,2,3,5})
                    {Vector3 p=child.TransformPoint(vertices[i+offset]);Check(Mathf.Abs(p.y-WorldLayout.GroundHeight(p)+.015f)<.001f,"Seagrass roots meet the seabed");}
                }
            }
            Check(reefs>40 && reefClearance>=.799f,"Reef gardens stay submerged, including tall coral");
            var foundation=GameObject.Find("Cottage foundation").GetComponent<MeshFilter>().sharedMesh;
            foreach(var vertex in foundation.vertices)
                Check(Mathf.Abs(vertex.y-2.45f)<.001f || Mathf.Abs(vertex.y-WorldLayout.GroundHeight(vertex)+.12f)<.001f,"Cottage foundation connects its raised floor to terrain");
            Check(GameObject.Find("Veranda ground supports")!=null,"Raised veranda has ground supports");
            var water=OceanSimulation.Surface.GetComponent<MeshFilter>().sharedMesh;
            Check(water.bounds.extents.x-10-BoatFollowCamera.MaxDistance*1.4f>270,"Animated water covers the view beyond full sea haze");
            Check(water.vertexCount<85000,"Far water coverage preserves the dense near grid with bounded geometry");
            foreach(var school in game.Schools)Check(school.MinimumSeabedClearance>=.449f,"Initial fish fit above the sloped seabed");
            ValidateWildlife(game);
            Debug.Log($"VOYAGE_ENVIRONMENT_TESTS_PASSED: coast/collider/seabed agreement, coral={reefs} rooted and submerged (min top depth {reefClearance:F2}m), seagrass roots, cottage foundation/veranda, water coverage/vertex budget, fish seabed clearance, wildlife seabed/fade/breath/breach/absorption");
        }

        static void ValidateWildlife(GameBootstrap game)
        {
            foreach(var animal in game.Animals)
            {
                animal.Begin(new Vector3(-180,0,-175),new Vector3(170,0,-175),75);
                bool jumped=false,breathed=false;float previousVelocity=0;bool previousAirborne=false;
                for(int step=0;step<749;step++)
                {
                    float previousY=animal.Root.position.y;
                    animal.Tick(.1f,step*.1f,game.Boat.Root.position,game.Effects);
                    Check(animal.Root.position.y-WorldLayout.GroundHeight(animal.Root.position)>=animal.SeabedClearance-.001f,"Wildlife cannot dive through the seabed when leaving");
                    if(animal.Airborne)
                    {
                        float velocity=(animal.Root.position.y-previousY)/.1f;
                        if(previousAirborne)Check(Mathf.Abs((velocity-previousVelocity)/.1f+9.81f)<.02f,"Dolphin breach follows gravity");
                        previousVelocity=velocity;jumped=true;
                    }
                    previousAirborne=animal.Airborne;
                    if(animal.HasSpouted && !breathed)
                    {
                        Vector3 blowhole=animal.Root.TransformPoint(new Vector3(0,.72f,1.78f));
                        Check(blowhole.y>OceanSimulation.Height(blowhole,step*.1f)+.039f,"Whale breath starts above the waterline at its blowhole");breathed=true;
                    }
                }
                if(animal.Kind==MarineKind.Dolphin)Check(jumped,"Dolphin still breaches during an encounter");
                if(animal.Kind==MarineKind.Whale)Check(breathed,"Whale surfaces to breathe");
                var properties=new MaterialPropertyBlock();animal.Root.GetComponentInChildren<Renderer>().GetPropertyBlock(properties);
                Check(properties.GetFloat("_Aquatic")==1 && properties.GetFloat("_EncounterVisibility")<.01f,"Submerged wildlife absorbs light and fades before pool release");
                // Inspect the real skin throughout a complete swim cycle at the lowest allowed height.
                var skin=animal.Root.GetComponentInChildren<SkinnedMeshRenderer>();var mesh=new Mesh();var animation=animal.Root.GetComponentInChildren<Animation>();
                try
                {
                    foreach(string clip in new[]{"Swim","Glide"})
                    {
                        if(animation[clip]==null)continue;animation.Play(clip);
                        for(int pose=0;pose<24;pose++)
                        {
                            animation[clip].normalizedTime=(pose+.35f)/24f;animation.Sample();skin.BakeMesh(mesh);
                            foreach(var vertex in mesh.vertices)
                                Check(skin.transform.TransformPoint(vertex).y>WorldLayout.SeabedHeight+.02f,"Imported "+animal.Kind+" skin remains above the offshore floor during "+clip+" pose "+pose);
                        }
                    }
                }
                finally {animation.Play("Swim");UnityEngine.Object.Destroy(mesh);animal.Root.gameObject.SetActive(false);}
            }
        }
    }
}
