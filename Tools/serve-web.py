"""Serve the Unity Web build on localhost, including the correct WASM MIME type."""
import argparse
from functools import partial
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path


class UnityHandler(SimpleHTTPRequestHandler):
    extensions_map = {**SimpleHTTPRequestHandler.extensions_map, '.wasm': 'application/wasm', '.data': 'application/octet-stream'}

    def end_headers(self):
        self.send_header('Cache-Control', 'no-store')
        self.send_header('Cross-Origin-Opener-Policy', 'same-origin')
        self.send_header('Cross-Origin-Embedder-Policy', 'require-corp')
        super().end_headers()


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--root', type=Path, default=Path(__file__).resolve().parent.parent / 'IslandVoyage' / 'Builds' / 'Web')
    parser.add_argument('--port', type=int, default=8765)
    args = parser.parse_args()
    root = args.root.resolve()
    if not (root / 'index.html').is_file():
        parser.error(f'Web build has not been produced: {root}')
    server = ThreadingHTTPServer(('127.0.0.1', args.port), partial(UnityHandler, directory=str(root)))
    print(f'Island Voyage: http://127.0.0.1:{args.port} (root: {root})', flush=True)
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        server.server_close()
