# Spotify 集成设置指南

## 前置条件
- 一个 Spotify Premium 账户（必须）
- Spotify Web Playback SDK 需要 Premium 账户才能使用

## 步骤 1：创建 Spotify 开发者应用

1. 访问 [Spotify for Developers 仪表盘：https://developer.spotify.com/dashboard

2. 使用你的 Spotify 账户登录

3. 点击 **Create app（创建应用）

4. 填写应用信息：
   - **App name**（应用名称）：`.NET Music Hub`（或你喜欢的任何名称）
   - **App description**（应用描述）：`Personalized music recommendation system`
   - **Redirect URI**（重定向URI）：`http://localhost:5175/callback`（非常重要！必须完全匹配）
   - **Which API/SDKs are you planning to use?**（你计划使用哪些API/SDK？）：
     - Web Playback SDK
     - Web API
     - iOS SDK（可选）

5. 勾选协议同意条款

6. 点击 **Save**（保存）

## 步骤 2：获取应用凭证

1. 创建应用后，点击 **Settings**（设置）

2. 复制你的 **Client ID**，保存下来

3. 点击 **View client secret**，复制 **Client Secret**，保存下来

## 步骤 3：配置应用设置

在项目中找到配置文件：`src/MusicRec.Web/appsettings.json

将你的凭证填入：

```json
{
  "Spotify": {
    "ClientId": "你的ClientId",
    "ClientSecret": "你的ClientSecret",
    "RedirectUri": "http://localhost:5175/callback",
    "ApiBaseUrl": "https://api.spotify.com/v1/",
    "AccountsBaseUrl": "https://accounts.spotify.com/",
    "ShowDialog": false
  }
}
```

**注意：
- 不要提交包含真实凭证到版本控制中！
- 可以在部署时使用环境变量或安全配置

## 步骤 4：启动应用

1. 启动后端服务（如果还没启动）

2. 启动 Web 应用：`dotnet run --project src/MusicRec.Web`

3. 在浏览器访问 http://localhost:5175

4. 登录或注册账户

5. 在侧边栏底部点击 **连接 Spotify**

6. 按提示完成授权

## 步骤 5：测试播放功能

1. 授权成功后，你可以：
   - 在推荐、搜索任意歌曲
   - 点击播放按钮
   - 在播放器中控制播放
   - 调节音量
   - 跳转进度

## 功能说明

### 已集成的功能：
- Spotify Web Playback SDK
- OAuth 2.0 授权流程
- 歌曲完整播放（需要 Premium）
- 播放、暂停、音量控制
- 进度条控制
- 用户偏好记录

### 需要的权限：
- streaming
- user-read-email
- user-read-private
- user-read-playback-state
- user-modify-playback-state

## 常见问题

### Q: 为什么需要 Premium？
A: Spotify Web Playback SDK 只对 Premium 用户开放，免费用户无法使用此功能。

### Q: 授权失败怎么办？
A: 请检查：
1. Redirect URI 是否与应用设置完全匹配
2. Client ID 和 Client Secret 是否正确
3. 是否已登录 Spotify Premium

### Q: 播放器显示"设备不可用？
A: 可能是浏览器不支持，或者需要等待 SDK 初始化。

### Q: 凭证安全吗？
A: 使用 OAuth 2.0 安全流程，凭证只在服务器端处理，不会暴露在浏览器中。

### Q: 如何在生产环境部署？
A: 生产环境需要：
1. 更新 Redirect URI 为生产域名
2. 使用环境变量或密钥管理
3. 使用 HTTPS
4. 域名白名单在 Spotify 后台配置

## 技术支持

如有问题，请检查：
1. 浏览器控制台错误
2. .NET 日志输出
3. Spotify 开发者后台应用设置

---
