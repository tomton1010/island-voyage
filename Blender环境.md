# Blender 建模环境

安装日期：2026-10-02（北京时间）。

已安装 **Blender 5.2.2 LTS、Windows x64 官方免安装版**，并确认中文建模界面正常启动。

## 免费使用

Blender 对个人免费，也允许用于商业游戏、美术和动画。自己制作的模型、图片、动画及 .blend 文件可以自行使用。软件不需要购买许可或注册账号；第三方付费素材和插件另按各自许可处理。

官方说明：https://www.blender.org/about/license/

## 启动与路径

- 桌面及开始菜单快捷方式：**Blender 5.2 LTS**。
- 程序：`E:\Blender\blender-5.2.2-windows-x64\blender.exe`。
- 安装包缓存：`D:\codxWorkspace\3D island\Tools\Downloads\blender-5.2.2-windows-x64.zip`。
- 环境记录：`Tools\BlenderEnvironment.json`。
- 启动脚本：`Tools\Start-Blender.ps1`。

从本工作区运行：

```powershell
.\Tools\Start-Blender.ps1
.\Tools\Start-Blender.ps1 -File '.\Tools\BlenderValidation\Environment.blend'
```

Blender 自带 Python，无需另装 Python 或额外插件才能开始建模。免安装版无需修改系统 PATH；桌面快捷方式和本项目脚本均可启动。

## 来源与校验

版本由 Blender 官方 LTS 页面和发布目录确认。主站线路过慢，最终使用 Blender 官方镜像列表中列出的南京大学镜像分段下载，合并后与官方 SHA-256 完全一致。

- 官方 LTS：https://www.blender.org/download/lts/
- 官方发布目录：https://download.blender.org/release/Blender5.2/
- 官方校验文件：https://download.blender.org/release/Blender5.2/blender-5.2.2.sha256
- 官方镜像列表：https://mirror.blender.org/release/Blender5.2/blender-5.2.2-windows-x64.zip?mirrorlist
- 实际下载：https://mirror.nju.edu.cn/blender/release/Blender5.2/blender-5.2.2-windows-x64.zip

```text
SHA-256: 3849d17a682cba006075aaa3f3597ecb5c9c30ec31035b2e092c53e40679b535
Blender: 5.2.2 LTS
Build hash: d13f752e3b9c
Build date: 2026-09-15
```

官方 ZIP 校验通过。Windows 的 Authenticode 查询返回 `UnknownError`，证书主体显示 Blender Foundation；没有将该查询记作签名验证通过。

## 实际验证

`Tools/Validate-Blender.py` 已通过以下操作，进程退出码为 0：

1. 创建三维网格与材质。
2. 保存 .blend 文件并重新打开，确认模型仍存在。
3. 导出 FBX 文件。
4. 使用 Cycles CPU 渲染输出 640×360 PNG。
5. 启动图形界面，确认三维视口、默认立方体、场景集合和中文菜单正常显示。

验证产物位于 `Tools/BlenderValidation`：`Environment.blend`、`Environment.fbx`、`Environment.png`、`Validation.json`、`Run.log`、`Error.log`。渲染检查使用 CPU；未进行复杂场景、GPU 渲染或性能基准。日志中的 `use_nodes` 提示为面向 Blender 6.0 的弃用预告，本版本验证正常完成。

## 与 Unity 项目配合

当前海岛游戏仍使用既有 C# 程序生成模型。安装 Blender 后，可在 Blender 中制作并保存精细模型，再导出 FBX、贴图等导入 Unity，配置材质、碰撞与动画；安装本身没有替换游戏里的现有模型。

建模时按米设置尺度，导出前应用旋转和缩放。以 FBX 为模型交换格式，避免依赖 Unity 自动调用不同版本的 Blender 转换 .blend 文件。验证产物是独立环境测试，没有加入游戏场景。
