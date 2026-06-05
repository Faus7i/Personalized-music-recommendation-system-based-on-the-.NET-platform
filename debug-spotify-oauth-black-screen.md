# Debug Session: spotify-oauth-black-screen
- **Status**: [OPEN]
- **Issue**: 点击“连接 Spotify”完成授权登录后页面黑屏/无跳转，无法完成账号绑定
- **Debug Server**: http://127.0.0.1:7787/event
- **Log File**: .dbg/trae-debug-log-spotify-oauth-black-screen.ndjson

## Reproduction Steps
1. 打开 Web：`http://localhost:5175`
2. 登录项目账号
3. 点击顶部“连接 Spotify”
4. 在授权页完成登录并同意授权
5. 观察是否出现黑屏、无法回跳、或站点断连/重载

## Hypotheses & Verification
| ID | Hypothesis | Likelihood | Effort | Evidence |
|----|------------|------------|--------|----------|
| A | `redirect_uri/state` 参数不一致或编码异常导致回调失败（Spotify 返回 error 或 code 无法交换） | High | Med | Pending |
| B | 回调页执行时 Blazor Server Circuit 未处理异常导致连接断开，表现为黑屏/重载 | High | Med | Pending |
| C | `ProtectedLocalStorage` 写入/读取在回调时失败（浏览器存储不可用/权限/时机），导致会话未持久化并触发异常 | Med | Med | Pending |
| D | OAuth token 交换/刷新请求失败（网络、CORS、TLS、Spotify 返回 4xx/5xx），异常未被前端兜底展示 | Med | Med | Pending |
| E | 授权页/回调页的强制刷新导航（forceLoad）与路由状态冲突，导致页面进入空白状态 | Low | Low | Pending |

## Log Evidence
(pending)

## Verification Conclusion
(pending)
