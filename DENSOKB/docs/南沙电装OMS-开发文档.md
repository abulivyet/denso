# 南沙电装 OMS 系统 — 开发文档

**系统名称：** 南沙电装 OMS（订单 / 物流单据处理系统）  
**工程名：** DENSOKB  
**入口页：** `login.html`（登录成功进入 `index.html`）  
**文档用途：** 开发、维护对照代码、接口、库表与文件目录  
**说明：** 左侧菜单文案以现场库表 `menu` / `role_menu` 为准，本文用页面文件名和功能名描述。

---

## 1. 系统做什么

主机厂把订单、看板、传票、取货计划等以 Excel / PDF / zip / rar 发给南沙电装。OMS 按客户版式自动：

1. 识别文件属于哪家客户、哪类单据  
2. 解压或拆 PDF / 读 Excel  
3. 对照原单位表（品番、收容数、托箱数）填确认表、标签  
4. Excel 转 PDF、按捆包/订单拆文件、合并成套票  
5. 把生成文件路径写入数据库，供页面查询和下载打印  

不是进销存，而是按客户定制的**单据工厂**。

### 1.1 客户一览

| 代码 | 客户 | 量产输入 | 其它输入 | 主要产出 |
|------|------|----------|----------|----------|
| FTCE | 长春（一汽丰田） | `.rar` | 文件名含 `FTMS` 的补给 Excel | 捆包、货垛、受领书、小纸张/A4、确认表 |
| FTET | 一汽发动机 | `.zip` | 补给 zip（名含 BJ）、直链配货 Excel、集货计划 | 受领书、码垛单、看板、链标签、确认表、核对记录 |
| DHEC | 东风本田 | 名含 `order` 的 Excel | `PLAN` 取货计划；看板/传票 PDF | 日计划成套：确认表、箱标签、套票 |
| CHAM | 佛山本田 | 周订单 xlsx | 传票/送货单/托标签/看板 PDF | 确认表、传票、托标、看板、指示单 |
| GHAC | 广州本田 | 名含 GHAC 的周订单 Excel | 取货计划/SP 计划；多种 PDF | 按指示号合成套：传票、PLS、托盘、指示书、追溯、确认表 |

---

## 2. 技术栈与运行环境

| 项 | 说明 |
|----|------|
| 运行时 | .NET Framework 4.7.2，**必须 x64**（LibreOffice CLI、部分 PDF 组件） |
| 宿主 | IIS / IIS Express，Web API 2 |
| 路由 | `{controller}/{action}`，无 `{id}` 默认段；属性路由也开了 |
| 前端 | ExtJS 5（Neptune 主题），`page/*.html` + `Scripts/js/*.js` |
| 数据库 | MySQL，库名 `omsdb`，连接串在 `Web.config` 的 `connectionString` |
| 认证 | JWT，HMAC-SHA256，密钥当前为代码内 `your-secret-key`，有效期 1 小时 |
| Excel | NPOI（HSSF=.xls，XSSF=.xlsx） |
| PDF | Spire.PDF（拆/合/抽字/填模板）、iText / iTextSharp |
| Excel→PDF | 站点内嵌 LibreOffice：`libreoffice/program/soffice.exe`，UNO Bootstrap |
| 压缩 | SharpCompress（zip、rar） |
| 日志 | `Helper/XErrorLogToolMDM.cs` → `{站点}\ChyLog\{yyyyMMdd}\`、`ErrorLog\` |

请求体默认 JSON。上传用 `multipart/form-data`。`httpRuntime maxRequestLength` 与 IIS `requestLimits` 已放大，可传大压缩包。

---

## 3. 工程目录

```
DENSOKB/
├── App_Start/            WebApiConfig、RouteConfig、BundleConfig
├── Controllers/          Web API（按客户拆 Controller）
├── Libs/                 Service、JWT 过滤器、baseMessage
├── Models/               DTO（FtceDto、OrderDownload、Confirm 等）
├── Helper/               DataHelper、SpireHelper、LiberOfficeHelper、PulicHelper
├── page/                 业务页面
├── Scripts/js/           页面对应脚本；index.js 为主框架
├── login.html / index.html
├── UploadFiles/          模板 + GHAC 拆分结果 + 原单位表
├── Uploads/              用户上传的原始文件（按客户分子目录）
├── libreoffice/          内嵌 LibreOffice 运行时
├── program/              另一份 LibreOffice 运行文件
├── Web.config
└── docs/                 本文档
```

分层习惯：Controller 接请求 → `Libs/*Service` 查库 → `Helper` 做文件。部分客户逻辑很长，直接写在 Controller（`FTETdisposeController`、`GHACController`）。

### 3.1 前端页面对照

| 页面文件 | 脚本 | 典型用途 |
|----------|------|----------|
| `login.html` | 页内脚本 | 登录 |
| `index.html` + `Scripts/js/index.js` | 主框架 | 顶栏、左侧菜单、iframe 打开子页 |
| `page/orderImport.html` | 页内 + `uploadFiles.js` | 长春 / 一汽 订单导入、标签下载 |
| `page/orderHWork.html` | `orderHWork.js` | 本田三家原始文件导入 |
| `page/orderCHAMweek.html` | `orderCHAMweek.js` | 佛山周订单 |
| `page/orderCHAMout.html` | `orderCHAMout.js` | 佛山出货上传/下载 |
| `page/orderDHECGoods.html` | `orderDHECGoods.js` | 东风周订单 |
| `page/orderDHECdayplan.html` | `orderDHECdayplan.js` | 东风日计划下载 |
| `page/orderGHACweek.html` | `orderGHACweek.js` | 广本周订单 |
| `page/orderGHACdayplan.html` | `orderGHACdayplan.js` | 广本取货计划成套下载 |
| `page/chain.html` | `chain.js` | 一汽直连链标签数据查询 |
| `page/unit.html` | `unit.js` | 原单位表导入导出 |
| `page/user.html` | `user.js` | 用户 |
| `page/user_role.html` | `user_role.js` | 角色、角色菜单 |
| `page/menu.html` | — | 菜单维护 |
| `page/roleMenu.html` | — | 角色菜单 |

主框架取菜单：`GET /role/GetRoleMenu?user={登录名}`。`url` 为空的节点只作分组，不打开页。

---

## 4. 认证

1. `POST /Login/login`，body：`{ username, password }`。密码先 MD5 再与 `user` 表比对。  
2. 成功 `code=200`，`data.token`、`data.name`。前端写入 `localStorage.token`、`localStorage.user`（存的是 **name**）。  
3. 业务接口加 `[JwtAuthorize]`，请求头：`Authorization: Bearer {token}`。  
4. 过期或无头返回 401，前端弹「请先登录」并跳 `login.html`。  
5. 密钥与过期校验在 `Libs/JwtAuthorizeAttribute.cs`，须与 `LoginController.GenerateToken` 一致。

未加 `[JwtAuthorize]` 的接口（上传解析内部互调、部分 GHAC 上传）可被直接调用，改接口时注意。

通用返回：

- 列表：`{ success, total, data }`  
- 业务：`ResParameter`：`code`（200 成功）、`info`、`data`、以及解析时的 `ordercode` / `bin` / `fdate` / `ddate`  
- 上传：`baseMessage`：`success`、`result`、`message`

---

## 5. 主流程（端到端）

```
用户在导入页点「上传文件」
  → POST /orderImport/fileNameCheck   （MD5 判重）
  → POST /orderImport/UploadFile      （FormData: files + username）
  → 按扩展名 / 文件名 / PDF 正文识别客户
  → 保存到 MapPath("~/Uploads")/{子目录}/文件名
  → 写 orderimport（handlestate=1，redundancy1=绝对路径）
  → Task.Run 调 uploadController.analysisAPI_batch
       → 按 getApiPath 拼 apiPath + 相对地址
       → POST JSON { zipPath, basePath=fileUpload, createPerson, mainId }
       → 客户 dispose 接口解压、拆页、填模板、转 PDF
       → 明细写入 ord_inp_detail2（Stacklabel=生成文件绝对路径）
       → 确认表路径再写入 confirm
       → 回写 orderimport：handlestate 2/3，订单号、发货日、耗时
  → 用户刷新列表，勾选下载
```

`handlestate`：`1` 解析中 / `2` 成功 / `3` 失败 / `4` 重新解析中。

重新解析：`POST /orderImport/order_Re_analyze` → 先改状态、删 `ord_inp_detail2`，再走同一套解析。

---

## 6. 三套文件根目录（开发时不要混）

| 根 | 代码取值 | 存什么 |
|----|----------|--------|
| 站点根 | `MapPath("~/")` / `BaseDirectory` | 程序、LibreOffice、相对路径模板 |
| `~/Uploads` | `MapPath("~/Uploads")` | **原始上传**。`SaveFileToFolder` 按客户分子目录 |
| `fileUpload` | `Web.config` **绝对路径** | FTET/FTCE/DHEC/CHAM **解析输出**。dispose 的 `basePath` 就是它 |

GHAC 拆分写在 `MapPath("~/UploadFiles/GHAC")`，**不走** `fileUpload`。  
若现场把 `fileUpload` 配成站点下的 `UploadFiles`，则与 GHAC 同一棵树；若配成独立盘符目录，就是两套数据盘。

`orderimport.redundancy1`、`ord_inp_detail2.Stacklabel` 等存的是**当时的绝对路径**。换盘不能只改 IIS，还要改配置和库（见运维迁移说明，本文不展开）。

### 6.1 上传落盘子目录（Uploads）

由 `orderImportController.SaveFileToFolder(fileName, bytes, uploadPath, Cust)` 决定，返回完整文件路径写入 `redundancy1`。同名则在文件名后加时间戳。

| Cust 参数 | 何时 |
|-----------|------|
| `FTCE` | `.rar` |
| `FTET` | `.zip` |
| `FTCE补给订单` | 名含 FTMS 的 xls/xlsx |
| `FTCE直链配货` | 名含「直链配货」（代码里目录名如此） |
| `GHAC` | GHAC Excel / 广本相关 PDF |
| `DHEC订单` / `DHEC\订单` | 东风 order Excel |
| `取货计划` / `DHEC\取货计划` | 计划类 Excel |
| `CHAM\周订单` | 「销售订单工作台整单导出」 |
| `CHAM\传票` / `送货单` / `托标签\temp` / `看板` | 对应 PDF |
| `DHEC\看板` / `DHEC\传票` | 东风 PDF |
| `TEMP` | PDF 识别前的临时副本 |

### 6.2 解析输出（fileUpload 下）

| 客户 | 典型目录 |
|------|----------|
| FTET | `{fileUpload}\FTET\{压缩包名}\`，直链子目录 `直链\`，核对记录 `{fileUpload}\FTET\核对记录表.xlsx` |
| FTCE | `{fileUpload}\FTCE\{压缩包或 Excel 名}\` |
| DHEC | `{fileUpload}\DHEC\看板`、`传票`、`TP\套票.pdf`、`download` |
| CHAM | 拆分多在 Uploads 源文件旁边；套票 `{fileUpload}\CHAM\TP\` |
| GHAC | `{站点}\UploadFiles\GHAC\SUMMONS`、`PALLET`、`PLS`、`INSTRUCTIONS`、`TRACE`、`TRACE\USED`、`output`、`download`；追溯状态 `_state_*.txt` |

---

## 7. 上传识别规则（`UploadFile`）

按 **扩展名** 进入分支，再按 **文件名关键字** 或 **PDF 正文** 细分。关键字区分大小写（`IndexOf` 默认大小写敏感），客户文件名不要改。

### 7.1 压缩包与 Excel

| 条件 | cuscode | redundancy2 | 随后 |
|------|---------|-------------|------|
| `.rar` | FTCE | 量产 | `FTCElcPath` |
| `.zip` 且文件名含 `BJ` | FTET | 补给 | `FTETPath` |
| `.zip` 其它 | FTET | 量产 | `FTETPath` |
| xls/xlsx 含 `FTMS` | FTCE | 补给 | `FTCEbjPath` |
| 含「直链配货」 | FTET | 直链配货 | `setZL`，状态直接 2 |
| 含 `GHAC` 的 xls | GHAC | 周订单 | `GHACUpload(EXCEL)` |
| 含 DHEC/GHEC 且 `order` | DHEC | 量产订单 | `setWorkOrder`，状态 2 |
| 含 DHEC/GHEC 且 `PLAN` | DHEC | 取货计划 | `setDHECPlan`，状态 2 |
| 含「集货计划」 | FTET | 集货计划 | `ExecPlanExcel`，状态 2 |
| 含「销售订单工作台整单导出」 | CHAM | 周订单 | `saveWeekOrder` |
| 含「取货计划」或「SP计划」 | GHAC | 取货计划 | `GHACUpload(EXCEL)` → 内部再判八千代/阶段品走 PLAN2 |

`.xlsx` 里 `DHEC`+`PLAN` 另有分支，落到 `DHEC\取货计划`。

### 7.2 PDF（`SpireHelper.getPdfType`）

先看文件名：

- 含 `LABEL_D` → GHAC 看板 PLS  
- 含 `LABEL_T` → GHAC 托标签  

再扫 PDF 文本（按优先级）：

| 命中文字 | 类型 | 客户 |
|----------|------|------|
| `传票更改清单` 且无 `GHEC` | GHAC_01 | 广本 01 版（传票+指示书合订） |
| `发行顺序号` 且无取货指示书 | GHAC_TRACE | 追溯标签 |
| 同时有「取货指示书」和「送货传票」 | GHAC_01 | 合订 |
| 仅「取货指示书」 | GHAC_INSTRUCTIONS | |
| 「送货传票」+ CHAM | CHAM_SUMMONS | 佛山传票 |
| 「送货传票」+ GHAC | GHAC_SUMMONS | 广本传票 |
| 「送货传票」+ DHEC | DHEC_D_SP | 东风补给传票 |
| `托标签-CHAM` | CHAM_T | |
| `部品明示卡` | CHAM_D | 佛山看板 |
| `出货指示单` | CHAM_INSTRUCTIONS | 佛山送货单 |
| `零件明示卡` | DHEC_D | 东风看板 |

无法识别 → 前端提示「不能识别的 PDF 类型」。

内部解析地址由 `uploadController.getApiPath` 根据 `cuscode` + `redundancy2` 读取 Web.config。

---

## 8. 各客户解析要点

### 8.1 FTCE 量产 — `FTCEdisposeController.CompressedFileFTCE`

1. 用 SharpCompress 解 **rar** 到 `{basePath}\FTCE\{压缩包名}`。  
2. 找文件名以「捆包 List」结尾的 Excel：读看板张数、建捆包文件夹、Excel 转 PDF。  
3. 「货垛标签」Excel：按 Sheet 名对应捆包文件夹拆 PDF（LibreOffice）。  
4. 看板 Excel 拆小纸张 PDF，再按张数合并。  
5. 受领书 Excel 转 PDF。  
6. 用 `FTCEQRB` 模板生成确认表，路径写入 `confirm`。  
7. 明细 `filetype`：捆包、码垛单、小纸张、A4、受领书、确认表。

### 8.2 FTCE 补给 — `FTECSuppliesController.DisposeFtceSupply`

源是 FTMS Excel。转 xlsx → 按 Sheet 拆 → 空白纳入受领书转 PDF → 对照原单位算箱数 → 外箱单（`FTCEWXD`/`FTCEWXDPDF`）→ 货垛（`FTCEMDD`）→ 确认表（`FTCEQRB`）。

### 8.3 FTET — `FTETdisposeController.CompressedFileFTET`

解 **zip** 到 `{basePath}\FTET\{包名}\`。按 zip 内文件名：

| 文件名特征 | 处理 |
|------------|------|
| 含 `BJ` 或 `POORDT`，且不以 `T` 结尾、不含 `KB` | 补给受领书，抽到货纳期等 |
| 同上但以 `T` 结尾 | 补给码垛单 |
| 含 `KB` | 看板 |
| 量产受领书 / 码垛 / A4 / 小纸 | `A4andMin` 等拆页 |
| zip 名含 `TB` | 有链标签，用 `TFTELian` 模板 + `ftet_chain` 表生成直链 PDF |

确认表用 `FTETQRB`。套票下载时若勾选「取套票」，会再生成核对记录表（`FTETHD`）。  
文件名含「集货」的 zip 直接成功返回，不拆。  
数量全 0 的补给订单：套票只含受领书。

链数据来自「直链配货」Excel，`setZL` 写入 `ftet_chain`。无链数据时解析消息会提示请先上传。

### 8.4 DHEC — `DHECdisposeController`

- Excel 订单 / PLAN：导入时已写入 `ord_weekorder` / `dhec_dayplan`，不必再异步解析。  
- PDF：`CompressedFileDHEC` 看是否含「零件明示卡」：有则拆到 `fileUpload\DHEC\看板`，无则当传票拆到 `DHEC\传票`，并记 `dhec_summons_order`。  
- **下载不读 Stacklabel**，按日计划里的传票号 **扫目录文件名**。SP 计划用 `DHECSP`（`DHEC_D.pdf`）填 PDF。

### 8.5 CHAM — `CHAMdisposeController`

| 接口 | 行为 |
|------|------|
| `saveWeekOrder` | 读周订单 xlsx 入 `cham_ord_week` |
| `DisposeSummonPdf` | 拆传票到源文件同名文件夹，入 `cham_court_summons` |
| `DisposeBoard` | 拆看板，按传票号前 14 位建夹，入 `cham_board.boardDictory` |
| `DisposeStackLabel` | 拆托标签，入 `chamStackLabel.inputPath` |
| `CompressedFileCHAM` → `disposeDeliveryNote` | 拆出货指示，匹配周订单；缺匹配视为补给；拼传票/看板/托标/确认表写 `ord_inp_detail2` |

`filetype`：传票、确认表、托标签、出货指示单、看板。

建议顺序：周订单 → 传票/看板/托标 → 最后送货单。

### 8.6 GHAC — `GHACController.GHACUpload`

| fileType | 方法 | 落到 |
|----------|------|------|
| EXCEL | `GHAC_EXCEL` / 内部再分周订单或计划 | `ord_weekorder` / `pickupplan` |
| SUMMONS | `GHAC_SUMMONS` | `UploadFiles\GHAC\SUMMONS\{品番}{版本}.pdf` |
| PALLET | `GHAC_PALLET` | `PALLET\` |
| PLS | `GHAC_PLS` | `PLS\`，3 列 2 行拼版 |
| INSTRUCTIONS | `GHAC_INSTRUCTIONS` | `INSTRUCTIONS\{指示号}.pdf` |
| TRACE | `GHAC_TRACE` | `TRACE\` |
| GHAC01 | `GHAC_01` | 一份 PDF 里同时拆传票和指示书 |
| PLAN2 | `GHAC_PLAN2` | 八千代 / SP / 阶段品计划 |

成套输出 `GHACoutputByCode`：按取货指示号从上述目录 **按文件名匹配** 再 Merge。八千代走 `CreateBaQianDaiKB` / `CreateBaQianDaiT`，模板为 `UploadFiles\八千代KB模板.pdf`、`八千代T模板.pdf`。  
追溯用 `_state_{pn}_{日期}_{地点}.txt` 记录已取文件，用过的可挪到 `TRACE\USED`。

---

## 9. 明细 filetype 与下载勾选项对照

`ord_inp_detail2.filetype` 必须与下载过滤一致。

**长春 / 一汽（订单导入页「标签下载」）**

| 界面 | 提交 type 片段 | 库 filetype |
|------|----------------|-------------|
| 确认表 | check8 | 确认表 |
| 箱标签 + 小纸打印 | box + boxMin | 小纸张 |
| 箱标签 + A4 穿透 | box + boxA4 | 把小纸张拆开再按客户拼 A4（FTCE 量产 4 合 1，补给 3 合 1；FTET 3 合 1） |
| 托标签 | check2 | 码垛单 |
| 托标签 + 合并成一个文件 | check2+check3 | 多份码垛单 Merge |
| 纳品书 | check4 | 受领书 |
| 取套票 | check7 | 套票（合并）；FTET 另加核对记录表 |

行内图标：受领书 `sls`、托标签 `MDcode`、箱标签弹小纸/A4、确认单。

**佛山出货**

| 界面 | type | filetype |
|------|------|----------|
| 确认表 | check5 | 确认表 |
| 传票 | check1 | 传票 |
| 托标签 | check2 | 托标签 |
| 箱标签 | check3 | 看板 |
| 取货指示单 | check4 | 出货指示单 |
| 取套票 | check7 | 合并 |

**东风日计划：** check1 确认表、check2 箱标签、check7 取套票。必须同一 `carno`。

**广本取货计划：** CONFIRM / INSTRUCTIONS / SUMMONS / PALLET / PLS / TRACE / 取套票，按指示号从目录拼文件，不是只读 `ord_inp_detail2`。

---

## 10. API 一览

格式均为 `/{Controller}/{Action}`。需登录的已标 JWT。

### 10.1 登录与权限

| 方法 | 路径 | 说明 |
|------|------|------|
| POST | `/Login/login` | `{username,password}` → token |
| POST | `/Login/GetUserPageList` | 用户分页 |
| POST | `/Login/Adduser` | 新增 |
| POST | `/Login/pudatepswd` | 改密 |
| POST | `/Login/deluser` | 删除 |
| GET | `/role/GetRoleMenu?user=` | 菜单树（主框架） |
| POST | `/Role/GetRolePageList` | 角色列表 |
| POST | `/Role/Addrole` | 增角色 |
| POST | `/Role/SetUserRole` | 用户绑定角色 |
| POST | `/RoleMenu/GetRoleMenus` | 角色已选菜单 |
| POST | `/RoleMenu/SaveRoleMenus` | 保存角色菜单 |
| POST | `/Menu/GetTree` | 菜单树维护 |
| POST | `/Menu/Save` | 保存菜单 |

### 10.2 导入

| 方法 | 路径 | 说明 |
|------|------|------|
| POST | `/orderImport/fileNameCheck` | multipart，MD5 判重 |
| POST | `/orderImport/UploadFile` | multipart：`files`、`username` |
| POST | `/orderImport/GetPageList` | JWT，Pagination + queryJson |
| POST | `/orderImport/order_Re_analyze` | 重新解析 |
| POST | `/orderImport/delimport` | 删 orderimport + ord_inp_detail2 |
| POST | `/orderImport/SaveConfirmRemark` | 确认表备注 |
| GET | `/orderImport/GetConfirmRemark` | 批量备注 |

Pagination 常见字段：`page`、`start`、`limit`、`queryJson`、`sidx`、`sord`。

### 10.3 解析（内部 HTTP，body 为 FtceDto / ChamInputDto）

| 配置键 | 路径 |
|--------|------|
| FTETPath | `/FTETdispose/CompressedFileFTET` |
| FTCElcPath | `/FTCEdispose/CompressedFileFTCE` |
| FTCEbjPath | `/FTECSupplies/CompressedFileFTCESupplies` |
| DHECPath | `/DHECdispose/CompressedFileDHEC` |
| CHAMPath | `/CHAMdispose/CompressedFileCHAM` |
| CHAMDisposeSummonPdf | `/CHAMdispose/DisposeSummonPdf` |
| CHAMDisposeBoard | `/CHAMdispose/DisposeBoard` |
| CHAMDisposeStackLabel | `/CHAMdispose/DisposeStackLabel` |

FtceDto：`zipPath`、`basePath`、`createPerson`、`mainId`。

### 10.4 查询与下载

| 路径 | 说明 |
|------|------|
| `/DownloadFile/DownloadFileZipRowid` | FTET/FTCE 勾选打包 |
| `/DownloadFile/DownloadFileZipNew` | 其它打包 |
| `/DownloadFile/DownloadFileZipJS` | 集货相关 |
| `/CHAMdispose/GetPageList` | 佛山出货列表 |
| `/CHAMdispose/GetPageWeekList` | 佛山周订单 |
| `/CHAMdispose/DownloadFileZipRowid` | 佛山下载 |
| `/DHECdispose/GetPageDayPlanList` | 东风日计划 |
| `/DHECdispose/download` | 东风下载，body 为勾选行 JArray |
| `/GHAC/GetGHACPage_weekDT` | 广本周订单 |
| `/GHAC/GetGHACPage_planDT` | 广本取货计划 |
| `/GHAC/GHACoutputByCode` | 广本成套 |
| `/chain/GetPageList` | 直连链 |
| `/Unit/UploadFile` | 原单位导入，form 字段 `files` + `typeCode`（toyota/honda） |
| `/Unit/DownloadFile?type=` | 导出 toyota/honda 原单位表 |
| `GET /ExportRpt/getRpt?cuscode=&startdate=&enddate=` | 报表 |

注意：`DownloadFile/DownloadFile` 内有测试写死路径，业务不要走它。

---

## 11. 数据库（omsdb）

无独立建表脚本时以现场库为准。下面是代码实际用到的表。

### 11.1 `orderimport`（导入主表）

| 字段 | 含义 |
|------|------|
| id | 主键，即明细 mainId |
| inportdate | 导入时间 |
| cusname / cuscode | 客户中文名 / 代码 |
| ordercode / orderbin | 解析后订单号、便次 |
| podate | 发货日期 |
| createperson / createdate | 作成者（登录名）、作成时刻 |
| handlestate / handlemess | 状态、消息 |
| redundancy1 | **原始文件绝对路径** |
| redundancy2 | 类型：量产/补给/周订单/传票/送货单… |
| redundancy3 | 原始文件名 |
| redundancy4 | GUID |
| redundancy7 | 计划到货日等 |
| handlebegin / handleend / handletime | 解析耗时（秒） |
| hassupplies | 佛山是否补给 |
| filehash | MD5 判重 |
| downloadcount | 下载次数 |
| confirmRemark | 行备注（部分流程走 confirmbatch） |

### 11.2 `ord_inp_detail2`

| 字段 | 含义 |
|------|------|
| mainId | 导入 id |
| ordercode | 订单号 |
| filetype | 见第 9 节 |
| Stacklabel | **生成文件绝对路径**，多文件可用 `;` |
| remarks | 解析备注 |
| rowcode | 套票内排序，必须有 |

### 11.3 其它路径表

| 表 | 路径字段 | 写入时机 |
|----|----------|----------|
| confirm | excelpath, pdfpath | 生成确认表；改备注时打开 excelpath |
| cham_board | boardPath, boardDictory | 看板拆分 |
| chamStackLabel | inputPath | 托标签 |
| cham_court_summons | path | 传票单张 |

### 11.4 业务表（一般不存路径）

| 表 | 用途 |
|----|------|
| originalunit | 量表。Unit 导入时丰田清 FTEC/FTET，本田清 DHEC/CHAM/GHAC 再插入 |
| orderinfo | 解析出的零件行，报表用 |
| pickupplan | GHAC 取货计划 |
| ord_weekorder | GHAC/DHEC 周订单 |
| dhec_dayplan | 东风日计划（车组、传票、计划离开时间） |
| dhec_summons_order | 传票号↔订单号 |
| cham_ord_week | 佛山周订单 |
| ftet_chain | 直连 l1–l14 等链码 |
| ftet_plan | 集货计划 |
| confirmbatch | 按客户（+FTET 单号前两位）存批量备注 |
| user, role, user_role, menu, role_menu | 权限 |

原单位常用列：`cust_name`、`cust_code`、`cust_num`、`denso_num`、`back_num`、`box_cap`、`pallet_box`、`veh_model`、`part_name`、`factory` 等。解析标签缺数时优先查这张表。

---

## 12. Web.config 配置键

| 键 | 作用 |
|----|------|
| connectionString | MySQL |
| apiPath | 本站根 URL，内部再 POST 解析接口 |
| fileUpload | 解析输出绝对路径 |
| OriUnit | 相对路径，原单位文件目录 |
| TFTELian | `Uploads\FTET\FTET直连配货标签模板.xlsx`（注意在 Uploads） |
| FTETQRB | FTET 确认表模板 |
| TFTEQRB | 备用确认表 |
| FTCEQRB / FTCEMDD / FTCEWXD / FTCEWXDPDF | 长春确认表、货垛、外箱单 |
| CHAMQRB | 佛山确认表（广本确认表也用它） |
| DHECSP | `UploadFiles\DHEC_D.pdf` SP 标签 |
| FTETHD | 核对记录表模板 |
| TFTELLength | 直连最大连号，默认 14 |
| FTETPath 等 | 解析接口相对 URL |
| LicenseCategory / SpirePdfLicense | Spire 授权；debug 时 LoadLicense |

模板取值方式不统一：有的 `Path.Combine(BaseDirectory, 配置值)`，有的直接 `BaseDirectory + 配置值`。改路径时两种都要能拼对。

硬编码模板文件名（不在 config）：`UploadFiles\八千代KB模板.pdf`、`八千代T模板.pdf`。

---

## 13. 关键 Helper

| 类 | 职责 |
|----|------|
| DataHelper | Query / ExecuteSql；部分 SQL 字符串拼接 |
| SpireHelper | PdfSplit、Merge、MergeFiles、GetText、getPdfType、FillPdf、八千代看板 |
| LiberOfficeHelper | soffice 转 PDF、按 Sheet 拆货垛、UNO 导出 |
| PulicHelper | EditConfirmExcel、saveConfirm、GetOri/GetBoxCap |
| FileHelper | 另一套 Excel→PDF（含 wkhtmltopdf 分支，现网主路径是 LibreOffice） |
| HttpPost.PostXmlToUrl | 内部调解析接口 |

LibreOffice 路径：`BaseDirectory\libreoffice\program\soffice.exe`。用户配置在 `%APPDATA%\LibreOffice\4`，不在站点目录。UNO `Bootstrap.bootstrap()` 失败看 ChyLog。

---

## 14. 本地开发

1. VS 平台目标 x64；IIS Express 勾选 64 位。  
2. 站点需带完整 `libreoffice` 目录。  
3. `fileUpload`、`apiPath`、MySQL 改成本机。  
4. `UploadFiles` 下模板文件必须存在，否则确认表报「模板未找到」。  
5. 编译期 LibreOffice SDK 引用 `C:\Program Files\LibreOffice_25.2_SDK\sdk\cli\*.dll`，仅本机编译需要。  
6. 先导入 `originalunit` 再测解析。FTET 直连先导链 Excel。  
7. 调上传可用 `page/orderImport.html` 或 `example/upload.html`（后者可能写死 localhost 端口）。

---

## 15. 新增客户 / 改版式建议改动点

1. `orderImportController.UploadFile`：扩展名、文件名关键字。  
2. `SpireHelper.getPdfType`：新 PDF 特征字，注意与现有客户文字冲突。  
3. `uploadController.getApiPath` + Web.config 新 Path。  
4. 新 `XxxdisposeController`：解压/拆页/填模板，写 `ord_inp_detail2`，`filetype` 与前端勾选一致。  
5. 模板放 `UploadFiles`，键进 config。  
6. 成套下载若不能复用 `DownloadFileZipRowid`，仿 GHAC/DHEC 做专页。  
7. 量表若字段不同，扩 `originalunit` 与 `UnitController`。

---

## 16. 维护时已知点

- 识别靠文件名 `IndexOf`，客户改命名会分错客户。  
- 路径字段为绝对路径，换盘要改 `fileUpload` + SQL REPLACE。  
- DHEC/GHAC 下载大量靠**扫目录文件名**，不靠明细表。  
- 登录与部分 Service 为 SQL 拼接。  
- JWT 密钥明文；token 存 name 不是 username，菜单接口按 name 查，改用户表时注意。  
- `DownloadFile/DownloadFile` 测试残留。  
- Spire 授权依赖 Web.config，过期会出现水印或拆页失败。  
- 解析在 `Task.Run` 后台跑，IIS 回收应用池可能打断进行中的任务。  
