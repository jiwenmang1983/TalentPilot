# TalentPilot — 测试用例文档

> 本文件记录系统管理模块的所有测试用例。
> 小Q 执行测试，小P 更新状态，Mark 最终审批。

**版本：** v0.1
**日期：** 2026-05-08
**状态：** 🔴 规划中

---

## 测试环境

| 项 | 值 |
|---|---|
| 后端地址 | http://localhost:5000 |
| 前端地址 | http://localhost:3000 |
| 测试账号 | admin@talentpilot.com / Admin123456 |
| 数据库 | talentpilot (MySQL localhost:3306) |

---

## Phase 1: 数据库 & 认证

### TC-0101: 数据库连接验证
- **描述:** 验证 talentpilot 数据库 6 张表存在
- **类型:** 前置验证
- **步骤:** `mysql -h 127.0.0.1 -P 3306 -u root -pSandvik2026! talentpilot -e "SHOW TABLES;"`
- **预期:** 返回 Users/Roles/Departments/RolePermissions/OperationLogs/UserLoginAttempts
- **状态:** 🔴 待测试

### TC-0102: EF Core DbContext 编译验证
- **描述:** dotnet build 编译通过
- **类型:** 前置验证
- **步骤:** `cd /mnt/d/Git/TalentPilot/backend/TalentPilot.Api && dotnet build`
- **预期:** 编译成功，无错误
- **状态:** 🔴 待测试

---

## Phase 2: 登录认证（S-06）

### TC-0201: 正确账号登录
- **描述:** 使用正确用户名密码登录，获取 JWT Token
- **类型:** API
- **步骤:** POST /api/auth/login {username: "admin", password: "Admin123456"}
- **预期:** 返回 accessToken + refreshToken + user 信息，code=200
- **状态:** 🔴 待测试

### TC-0202: 错误密码登录
- **描述:** 使用错误密码登录
- **类型:** API
- **步骤:** POST /api/auth/login {username: "admin", password: "wrongpass"}
- **预期:** 返回 401，code=401
- **状态:** 🔴 待测试

### TC-0203: 登录失败锁定
- **描述:** 同一账号连续 5 次错误密码后被锁定
- **类型:** API
- **步骤:** 连续 5 次 POST /api/auth/login 错误密码
- **预期:** 第 5 次后返回 423 Locked，提示"账号已锁定"
- **状态:** 🔴 待测试

### TC-0204: Token 刷新
- **描述:** 使用 refreshToken 获取新的 accessToken
- **类型:** API
- **步骤:** POST /api/auth/refresh {refreshToken: "..."}
- **预期:** 返回新的 accessToken + refreshToken
- **状态:** 🔴 待测试

### TC-0205: 获取当前用户信息
- **描述:** 带有效 Token 获取当前用户信息
- **类型:** API
- **步骤:** GET /api/auth/me (带 Authorization: Bearer {token})
- **预期:** 返回当前用户 id/username/realName/role/department
- **状态:** 🔴 待测试

### TC-0206: 无 Token 访问受保护接口
- **描述:** 不带 Token 访问需要认证的接口
- **类型:** API
- **步骤:** GET /api/auth/me (不带 Token)
- **预期:** 返回 401 Unauthorized
- **状态:** 🔴 待测试

---

## Phase 3: 用户管理（S-01）

### TC-0301: 分页查询用户列表
- **描述:** 带 Token 分页查询用户
- **类型:** API
- **步骤:** GET /api/users?page=1&pageSize=20
- **预期:** 返回用户列表，包含分页信息（total/pageSize/page）
- **状态:** 🔴 待测试

### TC-0302: 按角色筛选用户
- **描述:** 按角色筛选用户列表
- **类型:** API
- **步骤:** GET /api/users?role=HR
- **预期:** 只返回 HR 角色的用户
- **状态:** 🔴 待测试

### TC-0303: 创建新用户
- **描述:** Admin 创建新 HR 用户
- **类型:** API
- **步骤:** POST /api/users {username/realName/email/roleId/departmentId}
- **预期:** 返回新用户 id，数据库中有记录
- **状态:** 🔴 待测试

### TC-0304: 编辑用户
- **描述:** Admin 修改用户信息
- **类型:** API
- **步骤:** PUT /api/users/{id} {realName/email/roleId}
- **预期:** 用户信息更新成功
- **状态:** 🔴 待测试

### TC-0305: 禁用用户
- **描述:** Admin 禁用用户（软删除）
- **类型:** API
- **步骤:** DELETE /api/users/{id} 或 PUT 设置 isActive=false
- **预期:** 用户 isActive=false，无法登录
- **状态:** 🔴 待测试

### TC-0306: 重置用户密码
- **描述:** Admin 重置用户密码
- **类型:** API
- **步骤:** POST /api/users/{id}/reset-password
- **预期:** 密码重置为临时密码，可登录
- **状态:** 🔴 待测试

### TC-0307: 非 Admin 无法管理用户
- **描述:** HR 角色尝试操作用户管理
- **类型:** API
- **步骤:** HR Token 调用 POST /api/users
- **预期:** 返回 403 Forbidden
- **状态:** 🔴 待测试

---

## Phase 4: 角色管理（S-02）

### TC-0401: 获取角色列表
- **描述:** 获取所有角色
- **类型:** API
- **步骤:** GET /api/roles
- **预期:** 返回 Admin/HR/用人经理 三个预置角色
- **状态:** 🔴 待测试

### TC-0402: 系统预置角色不可删除
- **描述:** 尝试删除系统预置角色
- **类型:** API
- **步骤:** DELETE /api/roles/{admin_id}
- **预期:** 返回 400 BadRequest，提示"系统预置角色不可删除"
- **状态:** 🔴 待测试

### TC-0403: 获取角色权限
- **描述:** 获取某角色的权限配置
- **类型:** API
- **步骤:** GET /api/roles/{id}/permissions
- **预期:** 返回该角色的所有权限项（PermissionKey/Type/Scope）
- **状态:** 🔴 待测试

### TC-0404: 更新角色权限
- **描述:** 更新角色的权限配置
- **类型:** API
- **步骤:** PUT /api/roles/{id}/permissions {permissions:[...]}
- **预期:** 权限更新成功
- **状态:** 🔴 待测试

---

## Phase 5: 部门管理（S-03）

### TC-0501: 获取部门树
- **描述:** 获取完整部门树形结构
- **类型:** API
- **步骤:** GET /api/departments
- **预期:** 返回树形结构，包含根部门和子部门
- **状态:** 🔴 待测试

### TC-0502: 创建子部门
- **描述:** 在父部门下创建子部门
- **类型:** API
- **步骤:** POST /api/departments {name: "研发部", parentId: "父部门id"}
- **预期:** 子部门创建成功，可在部门树中看到
- **状态:** 🔴 待测试

### TC-0503: 移动部门
- **描述:** 修改部门的父部门
- **类型:** API
- **步骤:** PUT /api/departments/{id} {parentId: "新父部门id"}
- **预期:** 部门移动到新父部门下
- **状态:** 🔴 待测试

### TC-0504: 删除有空子部门的部门
- **描述:** 删除有子部门的父部门
- **类型:** API
- **步骤:** DELETE /api/departments/{id}
- **预期:** 返回 400，提示"请先删除子部门"
- **状态:** 🔴 待测试

---

## Phase 6: 权限管理（S-04）

### TC-0601: 获取菜单权限项
- **描述:** 获取所有菜单权限项
- **类型:** API
- **步骤:** GET /api/permissions/menu
- **预期:** 返回树形菜单结构（system/job/resume/interview/report 等模块）
- **状态:** 🔴 待测试

### TC-0602: Admin 可见所有菜单
- **描述:** Admin 角色的菜单权限
- **类型:** API
- **步骤:** GET /api/permissions/menu (Admin Token)
- **预期:** 返回全部菜单
- **状态:** 🔴 待测试

### TC-0603: 用人经理菜单受限
- **描述:** 用人经理角色的菜单权限
- **类型:** API
- **步骤:** GET /api/permissions/menu (用人经理 Token)
- **预期:** 只返回部分菜单（report/observe 等，无 user/role/department/permission 管理）
- **状态:** 🔴 待测试

---

## Phase 7: 操作日志（S-05）

### TC-0701: 关键操作自动写日志
- **描述:** 执行关键操作后，验证操作日志记录
- **类型:** API
- **步骤:** 执行一次用户创建，然后 GET /api/operation-logs
- **预期:** 日志列表中有刚创建用户的记录（action=User.Create/userId/adminId）
- **状态:** 🔴 待测试

### TC-0702: 日志分页筛选
- **描述:** 按用户/操作类型/时间范围筛选日志
- **类型:** API
- **步骤:** GET /api/operation-logs?userId={id}&action=User.Create&startDate=2026-05-01
- **预期:** 返回符合筛选条件的日志
- **状态:** 🔴 待测试

### TC-0703: 日志详情查询
- **描述:** 查看单条日志的详细信息
- **类型:** API
- **步骤:** GET /api/operation-logs/{id}
- **预期:** 返回日志完整信息（包含 Detail JSON 解析后的内容）
- **状态:** 🔴 待测试

---

## Phase 8: 数据权限隔离

### TC-0801: HR 只能看自己的数据
- **描述:** HR 登录后访问 /api/users
- **类型:** API
- **步骤:** HR Token 调用 GET /api/users
- **预期:** 只返回 HR 自己创建的用户的记录
- **状态:** 🔴 待测试

### TC-0802: 用人经理只能看本部门数据
- **描述:** 用人经理登录后访问 /api/users
- **类型:** API
- **步骤:** 用人经理 Token 调用 GET /api/users
- **预期:** 只返回本部门成员的记录
- **状态:** 🔴 待测试

### TC-0803: Admin 可见全部数据
- **描述:** Admin 登录后访问所有数据
- **类型:** API
- **步骤:** Admin Token 调用 GET /api/users
- **预期:** 返回所有用户记录
- **状态:** 🔴 待测试

---

## Phase 9: 前端 E2E 测试

### TC-0901: 登录页面加载
- **描述:** 打开前端登录页，验证页面正常加载
- **类型:** 前端 E2E
- **步骤:** 打开 http://localhost:3000/login
- **预期:** 显示用户名/密码输入框和登录按钮
- **状态:** 🔴 待测试

### TC-0902: 登录成功跳转
- **描述:** 使用正确账号登录后跳转到首页
- **类型:** 前端 E2E
- **步骤:** 登录页输入正确账号密码，点击登录
- **预期:** 跳转到 http://localhost:3000/dashboard，URL 更新，无报错
- **状态:** 🔴 待测试

### TC-0903: 登录失败提示
- **描述:** 使用错误密码登录，显示错误提示
- **类型:** 前端 E2E
- **步骤:** 登录页输入错误密码
- **预期:** 显示"用户名或密码错误"，不跳转
- **状态:** 🔴 待测试

### TC-0904: 用户管理页面-列表
- **描述:** Admin 登录后进入用户管理页面
- **类型:** 前端 E2E
- **步骤:** 登录 → 侧边栏点击"用户管理"
- **预期:** 显示用户列表，有分页，有筛选器
- **状态:** 🔴 待测试

### TC-0905: 用户管理页面-创建用户
- **描述:** Admin 在用户管理页面创建新用户
- **类型:** 前端 E2E
- **步骤:** 点击"新建用户" → 填写表单 → 提交
- **预期:** 新用户出现在列表中，弹出成功提示
- **状态:** 🔴 待测试

### TC-0906: 角色管理页面
- **描述:** Admin 进入角色管理页面
- **类型:** 前端 E2E
- **步骤:** 登录 → 侧边栏点击"角色管理"
- **预期:** 显示角色列表（Admin/HR/用人经理），预置角色有"不可删除"标识
- **状态:** 🔴 待测试

### TC-0907: 部门管理页面-树形展示
- **描述:** Admin 进入部门管理页面
- **类型:** 前端 E2E
- **步骤:** 登录 → 侧边栏点击"部门管理"
- **预期:** 显示树形部门结构，可展开/折叠
- **状态:** 🔴 待测试

### TC-0908: 操作日志页面
- **描述:** Admin 进入操作日志页面
- **类型:** 前端 E2E
- **步骤:** 登录 → 侧边栏点击"操作日志"
- **预期:** 显示日志列表，可按用户/操作类型/时间筛选
- **状态:** 🔴 待测试

### TC-0909: 无权限用户菜单过滤
- **描述:** 用人经理登录后，菜单只有部分选项
- **类型:** 前端 E2E
- **步骤:** 用人经理账号登录
- **预期:** 侧边栏无"用户管理/角色管理/部门管理/权限管理"菜单
- **状态:** 🔴 待测试

### TC-0910: 前端权限控制-禁止访问
- **描述:** 用人经理尝试直接访问用户管理 URL
- **类型:** 前端 E2E
- **步骤:** 用人经理 Token，直接访问 http://localhost:3000/system/users
- **预期:** 跳转到 403 页面或无权限提示，不显示用户数据
- **状态:** 🔴 待测试

---

## 测试统计

| Phase | 测试用例数 | 通过 | 失败 | 待测试 |
|---|---|---|---|---|
| Phase 1 | 2 | 0 | 0 | 2 |
| Phase 2 | 6 | 0 | 0 | 6 |
| Phase 3 | 7 | 0 | 0 | 7 |
| Phase 4 | 4 | 0 | 0 | 4 |
| Phase 5 | 4 | 0 | 0 | 4 |
| Phase 6 | 3 | 0 | 0 | 3 |
| Phase 7 | 3 | 0 | 0 | 3 |
| Phase 8 | 3 | 0 | 0 | 3 |
| Phase 9 | 10 | 0 | 0 | 10 |
| **合计** | **42** | **0** | **0** | **42** |
