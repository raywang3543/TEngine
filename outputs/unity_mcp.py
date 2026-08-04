# -*- coding: utf-8 -*-
"""通过 unity-mcp relay 的 stdio JSON-RPC 调用 Unity 编辑器工具。

用法: python3 unity_mcp.py <tool_name> [params_json]
示例: python3 unity_mcp.py Unity_GetConsoleLogs '{"count":5}'
"""

import json
import select
import subprocess
import sys

RELAY = "/Users/ray/.unity/relay/relay_mac_arm64.app/Contents/MacOS/relay_mac_arm64"
INIT_TIMEOUT = 20
CALL_TIMEOUT = 120


def send(proc, obj):
    proc.stdin.write(json.dumps(obj) + "\n")
    proc.stdin.flush()


def read_response(proc, request_id, timeout):
    """读到匹配 id 的响应为止；跳过通知与日志行。"""
    while True:
        ready, _, _ = select.select([proc.stdout], [], [], timeout)
        if not ready:
            raise TimeoutError(f"等待响应 id={request_id} 超时")
        line = proc.stdout.readline()
        if not line:
            raise EOFError("relay 进程关闭了输出")
        line = line.strip()
        if not line.startswith("{"):
            continue
        try:
            msg = json.loads(line)
        except json.JSONDecodeError:
            continue
        if msg.get("id") == request_id:
            return msg


def main():
    tool = sys.argv[1]
    params = json.loads(sys.argv[2]) if len(sys.argv) > 2 else {}

    proc = subprocess.Popen(
        [RELAY, "--mcp"],
        stdin=subprocess.PIPE,
        stdout=subprocess.PIPE,
        stderr=subprocess.DEVNULL,
        text=True,
        bufsize=1,
    )
    try:
        send(proc, {
            "jsonrpc": "2.0", "id": 1, "method": "initialize",
            "params": {
                "protocolVersion": "2024-11-05",
                "capabilities": {},
                "clientInfo": {"name": "kimi-cli", "version": "1.0"},
            },
        })
        read_response(proc, 1, INIT_TIMEOUT)
        send(proc, {"jsonrpc": "2.0", "method": "notifications/initialized"})
        send(proc, {
            "jsonrpc": "2.0", "id": 2, "method": "tools/call",
            "params": {"name": tool, "arguments": params},
        })
        response = read_response(proc, 2, CALL_TIMEOUT)
        print(json.dumps(response.get("result", response), ensure_ascii=False, indent=2))
    finally:
        proc.kill()


if __name__ == "__main__":
    main()
