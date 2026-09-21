# 南沙电装 OMS 数据库说明

**库名：** omsdb  
**数据库：** MySQL 8.0  
**字符集：** 表级多为 utf8mb4，部分字段仍为 utf8mb3  
**来源：** `omsdb.sql`（Navicat 结构导出，2026-09-03）  
**说明：** 库内**没有外键**。表之间通过业务字段关联（如 `orderimport.id` ↔ `ord_inp_detail2.mainId`）。字段注释以库为准，括号内补充代码中的实际用途。

---

## 1. 库概览

共 **23 张表**，按业务分组：

| 分组 | 表 | 用途 |
|------|----|------|
| 导入与下载 | `orderimport`、`ord_inp_detail2`、`ord_inp_detail`、`confirm`、`confirmbatch`、`orderinfo` | 上传记录、解析后文件路径、确认表 |
| 量表 | `originalunit` | 品番、收容数、托箱数等，生成标签必用 |
| 广本 GHAC | `pickupplan`、`ord_weekorder` | 取货计划、周订单（东风周订单也用 `ord_weekorder`） |
| 东本 DHEC | `dhec_dayplan`、`dhec_summons_order` | 日计划、补给传票对照 |
| 佛山 CHAM | `cham_ord_week`、`cham_board`、`cham_court_summons`、`chamstacklabel` | 周订单、看板/传票/托标签路径 |
| 一汽 FTET | `ftet_chain`、`ftet_plan` | 直连链数据、集货计划 |
| 权限 | `user`、`role`、`user_role`、`menu`、`role_menu` | 登录与菜单 |

**存磁盘绝对路径的字段（换盘必须改）：**

| 表 | 字段 |
|----|------|
| orderimport | redundancy1 |
| ord_inp_detail2 | Stacklabel |
| ord_inp_detail | Stacklabel、A4Board、Board、comfirm、Receipt、bale、lian |
| confirm | excelPath、pdfPath |
| cham_board | boardPath、boardDictory |
| cham_court_summons | path |
| chamstacklabel | inputPath |
| ftet_chain | path |

---

## 2. 表关系（逻辑）

```
user ── user_role ── role ── role_menu ── menu

orderimport (id)
    ├── ord_inp_detail2.mainId     解析后各类型文件（现行）
    ├── ord_inp_detail.mainId      旧明细，代码已很少写
    ├── confirm.mainId             确认表 Excel/PDF
    └── orderinfo                  解析出的零件行（按 orderCode 等关联，无外键）

originalunit                       被各客户解析/确认表按品番、客户简称查询

pickupplan.p_code ── ord_weekorder.p_code     广本计划对周订单
dhec_dayplan.s_code                            东本日计划（按送货传票号覆盖导入）
dhec_summons_order                             东本补给传票号 ↔ 订单号

cham_ord_week                                  佛山周订单
cham_court_summons.code                        传票号 → 单张传票文件
cham_board.boardCode                           看板目录
chamstacklabel.shippingCode                    出货指示号 → 托标签文件
```

---

## 3. 核心表

### 3.1 orderimport（订单上传）

导入主表。一行对应一次上传的一个文件。

| 字段 | 类型 | 说明 |
|------|------|------|
| id | int PK AI | 主键，明细 mainId |
| inportdate | datetime | 导入时间 |
| cusname | varchar(255) | 客户名称：长春、一汽发动机、东风本田、佛山本田、广州本田 |
| cuscode | varchar(255) | 客户代码：FTCE、FTET、DHEC、CHAM、GHAC |
| ordercode | varchar(255) | 解析后订单号 |
| orderbin | varchar(255) | 订单便次 |
| podate | datetime | 发货日期 |
| createperson | varchar(255) | 作成者（登录用户名） |
| createdate | datetime | 作成时刻 |
| handlestate | varchar(50) | **1** 解析中 **2** 成功 **3** 失败 **4** 重新解析（库注释写 1/2/3，代码另有 4） |
| handlemess | varchar(1000) | 处理消息 / 失败原因 |
| redundancy1 | varchar(255) | **原始文件绝对路径** |
| redundancy2 | varchar(255) | 类型：量产、补给、周订单、传票、送货单、取货计划等 |
| redundancy3 | varchar(255) | 原始文件名 |
| redundancy4 | varchar(255) | GUID，导入批次标识 |
| redundancy5 / 6 | decimal(10,2) | 未使用 |
| redundancy7 | datetime | 计划到货日等 |
| redundancy8 | datetime | 未使用 |
| handlebegin / handleend | datetime | 解析起止 |
| handletime | decimal(10,2) | 耗时（秒） |
| hassupplies | varchar(10) | 佛山是否含补给：是 / 否 |
| confirmRemark | varchar(2000) | 行级确认表备注 |
| downloadcount | int | 下载次数 |
| filehash | varchar(50) | 文件 MD5，上传判重 |

### 3.2 ord_inp_detail2（下载套票文件路径）

现行解析明细。下载套票按 `mainId` + `filetype` 取 `Stacklabel`。

| 字段 | 类型 | 说明 |
|------|------|------|
| id | bigint PK AI | 主键（降序索引） |
| mainId | varchar(255) | 对应 orderimport.id |
| Stacklabel | varchar(1000) | **生成文件绝对路径**，多个可用分号分隔 |
| filetype | varchar(255) | 见下表 |
| ordercode | varchar(255) | 单据号 |
| remarks | varchar(1000) | 解析备注 |
| r1 | varchar(255) | 备用 |
| rowcode | int | 套票内排序，下载必填 |

**filetype 常见值：** 确认表、受领书、码垛单、小纸张、A4、捆包、链标签、传票、托标签、看板、出货指示单、套票。

### 3.3 ord_inp_detail（导入明细表，旧）

早期按列分路径（货垛、A4、看板、确认表、受领书、捆包、链标签）。当前主流程写 `ord_inp_detail2`，本表可视为历史。

### 3.4 confirm / confirmbatch

| 表 | 字段 | 说明 |
|----|------|------|
| confirm | mainId, excelPath, pdfPath | 某次导入生成的确认表文件；改备注时打开 excelPath |
| confirmbatch | cusname, type, remark | 按客户（一汽 type 如 NB/BJ）存默认备注 |

### 3.5 orderinfo（订单信息）

解析出的零件级数据，报表 `ExportRpt` 使用。

| 字段 | 说明 |
|------|------|
| shipmentDate / arrivalDueDate | 出荷日、到货纳期 |
| orderCode | 订单号 |
| inclusionNumber | 纳入番号 |
| customerPartNumber / dnPartNumber / backNumber | 客户品番、电装品番、背番 |
| capacity / boardCount / inclusionQuantity | 收容数、看板张数、纳入个数 |
| category | 分类（FTET、CHAM、补给品等） |
| planCode / ReceiptPlace | 计划代码、收货地点 |

---

## 4. 原单位表 originalunit

无主键。导入时按客户侧删除后全量插入（丰田清 FTEC/FTET，本田清 DHEC/CHAM/GHAC）。

| 字段 | 类型 | 说明 |
|------|------|------|
| cust_name | varchar(100) | 客户简称：FTEC、FTET、DHEC、CHAM、GHAC |
| cust_code | varchar(100) | 客户代码 |
| recv_port | varchar(100) | 受入口（广本常与供应商代码对应） |
| veh_model | varchar(100) | 车型 |
| back_num | varchar(100) | 背番 |
| recv_area / del_loc | varchar(100) | 受入区、交货地点 |
| denso_num / cust_num | varchar(100) | 电装品番、客户品番 |
| custNum | varchar(100) | 客户品番备用列 |
| part_name | varchar(100) | 品番名称 |
| box_cap | int | 箱收容数 |
| pallet_box | int | 托标准箱数 |
| box_type / loc_num | varchar(100) | 箱型号、库位 |
| pallet_pct | decimal(16,4) | 托占比 |
| comb_pallet | varchar(100) | 组合打托 |
| traceability_label | varchar(4) | 是否追溯标签 |
| product_name_cn | varchar(150) | 品名中文 |
| factory | varchar(50) | 厂别（号口、八千代、SP 等） |
| UseFlag | varchar(10) | 使用标记 |
| sort | int | 排序 |

---

## 5. 广本 GHAC

### 5.1 pickupplan（广本取货计划）

量产、SP、八千代、阶段品都写入此表。下载套票主要查本表。

| 字段 | 类型 | 说明 |
|------|------|------|
| id | int PK | |
| no | varchar(20) | 取货指示号；SP 常为日期+SP+供应商 |
| version | varchar(5) | 指示版本 |
| carno | varchar(12) | 取货车次 |
| line | varchar(20) | 线路 |
| supplier / suppler_sub | varchar(10)/varchar(5) | 供应商、子工厂 |
| factory | varchar(5) | 广本工厂 |
| plancode | varchar(10) | 计划代码 |
| transcode | varchar(10) | 物流商 |
| pickdate / picktime | date / varchar(16) | 取货日期时间 |
| p_code / p_version | varchar(20)/varchar(5) | 传票号、传票版本（SP 导入曾写死 00） |
| rec_location | varchar(10) | 收货地点 |
| squre / vertical | varchar(5) | 平面/垂直位 |
| pn / color | varchar(20) | 零件号、颜色 |
| recdate / rectime | date / varchar(16) | 收货日期时间 |
| planqty | int | 计划数量 |
| palletno | varchar(10) | 托笼台序号 |
| packcode / packunit / packqty / containers | | 包装相关 |
| weeks / category / roles / remark | | 周次、分类、交货规则、备注 |
| confirmRemark | varchar(2000) | 确认表备注 |
| createPerson / create_date | | 导入人、导入时间 |
| downloadcount | int | 下载次数 |
| Releasedate | datetime | 表头取货日期 |

量产导入按 `no` 删旧再插；SP 按 `p_code` 删旧再插（取消行若不在新文件中会残留）。

### 5.2 ord_weekorder（周订单）

广本、东风周订单共用。东风页面筛 `r_location like 'D%'`。

主要字段：供应商编号、收货/上线地点、零件号与颜色名称、计划代码、厂内/收货日期时间、订货数量、传票号码、版本、周次、分类、交货规则、订单编号、车型。

---

## 6. 东本 DHEC

### 6.1 dhec_dayplan（取货计划）

导入按 **s_code（送货传票号）** 删除后插入。计划变更时，新文件没有的旧传票行不会自动删。

字段与 Excel 列对应：路线组别、取货车组、目的地、计划到达/离开、取货传票号、作成时间、送货传票号、供应商、零件号、订单数量、取货数量、计划代码、计划/调整到货、落点、确认表备注、来源文件名、下载次数。

下载套票按勾选行的车组、传票号去 `fileUpload\DHEC\看板|传票` **按文件名找 PDF**，不读路径表。

### 6.2 dhec_summons_order

补给传票拆分时写入：summons_code ↔ orderCode。

---

## 7. 佛山 CHAM

| 表 | 说明 |
|----|------|
| cham_ord_week | 周订单。出货指示按物料、计划、日期、数量等匹配传票。有订单号的行才会被配到 |
| cham_court_summons | code=传票号，path=单张传票绝对路径 |
| cham_board | boardCode、boardDictory=该传票看板所在目录 |
| chamstacklabel | shippingCode=出货指示号，inputPath=托标签文件 |

周订单索引：`orderCode`。取消订单需在周订单页删除，否则出货仍会匹配到。

---

## 8. 一汽 FTET

### 8.1 ftet_chain（链数据）

直连配货 Excel 导入。生成链标签时按 ordercode、bei 取 l1～l14。

| 字段 | 说明 |
|------|------|
| ordercode / labeldate / supname / bei / loction | 订单号、标签日期、供应商、背番、位置 |
| l1 … l14 | 各链号 |
| remark | 其他记录 |
| path | 相关文件路径 |

### 8.2 ftet_plan（集货计划）

`ddate` + `orderno`。集货计划 Excel 导入。

---

## 9. 权限

| 表 | 字段要点 |
|----|----------|
| user | username 登录名，password 为 MD5，name 显示名（前端 localStorage.user 存 name，菜单按 name 查） |
| role | r_code、r_name、r_mark、r_delete |
| user_role | userid、roleid、isstop |
| menu | id、parentid、title、Url、IsLeaf、Expanded、IconCls、OrderIndex |
| role_menu | roleid、menuid；**无主键** |

无数据库外键；role_menu、originalunit、user_role 部分表约束较弱。

---

## 10. 使用与维护注意

1. **没有外键**，删 `orderimport` 时程序会顺带删 `ord_inp_detail2`；直接在库里删主表会留孤儿路径行。  
2. **originalunit 无主键**，重复品番会导致确认表/标签数量异常。  
3. **路径字段存绝对路径**，应用换盘需 UPDATE REPLACE，并同步 Web.config 的 `fileUpload`。  
4. `ord_inp_detail` 与 `ord_inp_detail2` 并存，新功能只维护 detail2。  
5. `handlestate` 以代码为准：1/2/3/4。  
6. 表注释与字段注释大部分可用；`hassupplies`、`filehash`、`createPerson` 等少数列库内无 COMMENT。  
7. 字符集混用 utf8mb3 / utf8mb4，路径或中文文件名异常时优先查这两类列。

---

## 11. 表清单（导出顺序）

cham_board，cham_court_summons，cham_ord_week，chamstacklabel，confirm，confirmbatch，dhec_dayplan，dhec_summons_order，ftet_chain，ftet_plan，menu，ord_inp_detail，ord_inp_detail2，ord_weekorder，orderimport，orderinfo，originalunit，pickupplan，role，role_menu，user，user_role。
