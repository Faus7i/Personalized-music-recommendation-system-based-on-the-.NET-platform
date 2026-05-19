[OPEN]

# Session: login-register-stuck

## Symptom
- 在登录页或注册页输入信息后点击提交
- 页面可能出现提示信息，但不会继续跳转，表现为“没有反应”

## Expected
- 登录成功后跳转到 `/cold-start`
- 注册成功后跳转到 `/cold-start`
- 失败时明确展示错误且不破坏后续交互

## Hypotheses
- A: `OnValidSubmit` 没有触发，问题停留在表单校验或事件绑定
- B: 提交事件触发后，在调用 API 前后抛出异常，异常只部分显示
- C: Identity API 实际返回失败或端口不通，前端停留在错误提示状态
- D: `Session.SetUserAsync` / 持久化登录态时抛异常，导致不跳转
- E: 提交后 Blazor circuit 断开，导航和渲染失效

## Plan
- 为 Login / Register / Session 持久化添加最小埋点
- 复现一次登录和注册流程，采集 pre-fix 证据
- 根据证据确认根因后再做最小修复

## Evidence
- pre-fix：在注册页输入第一个字符后，浏览器立即出现 `Reload`，Blazor circuit 断开
- pre-fix：详细错误显示 `System.ArgumentException: Object of type 'Microsoft.AspNetCore.Components.ChangeEventArgs' cannot be converted to type 'System.String'`
- pre-fix：异常发生在输入事件处理阶段，因此登录/注册按钮后续表现为“没有反应”
- fix：将登录页、注册页的表单输入从 `InputText + @bind-Value:event="oninput"` 改为原生 `input + @bind:event="oninput"`
- post-fix：注册新账号成功并跳转到 `/cold-start`
- post-fix：退出后重新登录成功并跳转到 `/cold-start`

## Conclusion
- 排除 A：`OnValidSubmit` 本身不是首要根因，问题在提交前的输入阶段就已触发
- 排除 C：Identity API 端口不可达不是本次核心问题，相关服务已正常响应
- 排除 D：`Session.SetUserAsync` 不是首要根因，修复输入绑定后注册、登录与跳转均已成功
- 确认 E/B：错误输入绑定导致运行时异常，进而打断 Blazor circuit，使表单后续无法交互
