# FineUI.Pro.AppBox.Dapper

FineUI.Pro.AppBox.Dapper 是 FineUI 官方应用系统样板。本仓库是该项目的唯一真相源，欢迎通过 Issue 和 Pull Request 参与技术讨论与改进。

## 依赖方式

项目文件已声明从公共软件包仓库获取的 NuGet 包 `FineUI.Pro`。正常联网构建时，包管理器会自动还原依赖；仓库不包含 FineUI.Core.dll、FineUI.Pro.dll、fineui-java.jar，也不包含 FineUI 框架源码。

## 构建

安装 Visual Studio 的 .NET Framework 4.8 开发工具后，在 Developer PowerShell 中运行：

```powershell
msbuild FineUI.Pro.AppBox.Dapper.sln /t:Restore /p:RestorePackagesConfig=true
msbuild FineUI.Pro.AppBox.Dapper.sln /t:Build /p:Configuration=Release
```

## 运行

这是 .NET Framework 4.8 的 ASP.NET WebForms 应用，**没有 `dotnet run` 入口**，只能用 IIS Express 承载：

1. 用 Visual Studio 打开 `FineUI.Pro.AppBox.Dapper.sln`（安装时需勾选「ASP.NET 和 Web 开发」工作负载）；
2. 直接按 F5 / Ctrl+F5 启动。项目已配置为 IIS Express + 经典管道，端口由 VS 分配、启动后在浏览器地址栏可见，形如 **http://localhost:端口/**。

**不需要授权文件**：本仓库引用的是公共 NuGet 包 `FineUI.Pro`（社区版），社区版不做授权校验，克隆下来就能直接跑。

## 项目说明

FineUI.Pro.AppBox.Dapper 是基于 FineUI.Pro（社区版） 的通用权限管理框架，包括用户管理、职称管理、部门管理、角色管理、角色权限管理等模块。

更新下载：https://fineui.com/fans/
博客文章：https://www.cnblogs.com/sanshi/p/9540862.html

### 注意

1. FineUI.Pro.AppBox.Dapper 作为演示程序，请不要直接用于真实项目。
2. FineUI.Pro.AppBox.Dapper 作为演示程序，版本之间不兼容，也不支持版本升级。

### 使用步骤

1. 用 VS2022 打开项目工程文件（FineUI.Pro.AppBox.Dapper.sln）；
2. 使用 database 目录来初始化数据库；
3. 打开 Web.config，配属数据库连接字符串（connectionStrings->MySQL）；
  - 对应于 PageBase.cs 代码中 GetDbConnection（） / ConfigurationManager.ConnectionStrings["MySQL"]
4. 运行（Ctrl+F5）！
5. 请使用管理员账号登陆网站（用户名：admin 密码：admin）。

### 知识储备

1. 本项目采用 Dapper 作为数据库连接工具，详情：https://github.com/DapperLib/Dapper
2. 如果尚未安装.Net Framework 4.8，请先安装 SDK：https://dotnet.microsoft.com/en-us/download/dotnet-framework

## 发布历史

各版本的更新内容见 [CHANGELOG.md](CHANGELOG.md)。

## 许可边界

本仓库中由合肥三生石上软件有限公司拥有著作权的示例或应用项目源代码采用 [MIT 许可证](LICENSE)。FineUI 各端框架源码、二进制软件包、内嵌的 FineUI.js 运行时以及 FineUI 名称、标识和商标不属于 MIT 授权范围，仍适用各自的商业或社区版许可。具体边界见 [NOTICE.md](NOTICE.md)。

## 参与贡献

请先阅读 `CONTRIBUTING.md`。安全问题请按 `SECURITY.md` 私下报告。
