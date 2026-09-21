# -*- coding: utf-8 -*-
"""Generate a formal Word database design document from omsdb.sql."""
from __future__ import annotations

import re
from pathlib import Path

from docx import Document
from docx.enum.section import WD_SECTION
from docx.enum.table import WD_CELL_VERTICAL_ALIGNMENT, WD_TABLE_ALIGNMENT
from docx.enum.text import WD_ALIGN_PARAGRAPH, WD_LINE_SPACING, WD_TAB_ALIGNMENT
from docx.oxml import OxmlElement
from docx.oxml.ns import qn
from docx.shared import Cm, Pt, RGBColor

# A4 content width: 21.0 - 3.0 - 2.5 = 15.5 cm
CONTENT_WIDTH = Cm(15.5)
NAVY = "1F4E79"
NAVY_RGB = RGBColor(0x1F, 0x4E, 0x79)
GRAY_LINE = "BFBFBF"
HEADER_FILL = "1F4E79"
ALT_FILL = "F5F7FA"
BLACK = RGBColor(0x00, 0x00, 0x00)
BODY = RGBColor(0x22, 0x22, 0x22)


EXTRA_FIELD = {
    ("orderimport", "handlestate"): "程序取值：1 解析中，2 成功，3 失败，4 重新解析（库注释仅写 1/2/3）。",
    ("orderimport", "redundancy1"): "原始上传文件的磁盘绝对路径，换盘须批量替换。",
    ("orderimport", "redundancy4"): "导入批次 GUID。",
    ("orderimport", "redundancy5"): "未使用。",
    ("orderimport", "redundancy6"): "未使用。",
    ("orderimport", "redundancy7"): "计划到货日等业务日期。",
    ("orderimport", "redundancy8"): "未使用。",
    ("orderimport", "hassupplies"): "佛山是否含补给：是 / 否。",
    ("orderimport", "filehash"): "文件 MD5，用于上传判重。",
    ("orderimport", "createperson"): "作成者，实际写入登录用户名。",
    ("ord_inp_detail2", "mainId"): "对应 orderimport.id。",
    ("ord_inp_detail2", "Stacklabel"): "生成文件的绝对路径，多个路径可用分号分隔；换盘须改。",
    ("ord_inp_detail2", "filetype"): "常见值：确认表、受领书、码垛单、小纸张、A4、捆包、链标签、传票、托标签、看板、出货指示单、套票。",
    ("ord_inp_detail", "mainId"): "对应 orderimport.id。现行主流程已改写 ord_inp_detail2。",
    ("confirm", "mainId"): "对应 orderimport.id。",
    ("confirm", "excelPath"): "确认表 Excel 绝对路径；修改备注时打开此文件。",
    ("confirm", "pdfPath"): "确认表 PDF 绝对路径。",
    ("cham_board", "boardName"): "看板文件名。",
    ("cham_board", "boardPath"): "看板文件绝对路径。",
    ("cham_board", "boardDictory"): "该传票看板所在目录（拼写同库字段）。",
    ("cham_board", "boardCode"): "看板/传票编码。",
    ("chamstacklabel", "inputPath"): "托标签文件绝对路径。",
    ("cham_court_summons", "path"): "单张传票文件绝对路径。",
    ("originalunit", "cust_name"): "客户简称：FTEC、FTET、DHEC、CHAM、GHAC。",
    ("originalunit", "back_num"): "背番。",
    ("originalunit", "custNum"): "客户品番备用列。",
    ("originalunit", "UseFlag"): "使用标记。",
    ("originalunit", "sort"): "排序。",
    ("pickupplan", "p_version"): "SP 导入历史版本曾写死为 00。",
    ("pickupplan", "createPerson"): "导入人。",
    ("pickupplan", "create_date"): "导入时间。",
    ("user", "username"): "登录名。",
    ("user", "password"): "密码，MD5 存储。",
    ("user", "name"): "显示名。前端缓存此字段，菜单权限按 name 查询。",
    ("ftet_plan", "ddate"): "计划日期。",
    ("ftet_plan", "orderno"): "订单号。",
    ("dhec_dayplan", "confirmRemark"): "确认表备注。",
    ("dhec_dayplan", "fileName"): "来源文件名。",
    ("ftet_chain", "path"): "相关文件路径。",
    ("role_menu", "roleid"): "本表无主键。",
}

TABLE_INTRO = {
    "orderimport": "导入主表。一行对应一次上传的一个文件，是解析明细、确认表路径的逻辑父表。",
    "ord_inp_detail2": "现行解析明细。下载套票按 mainId、filetype 读取 Stacklabel。新功能只维护本表。",
    "ord_inp_detail": "早期按列存放各类标签路径。当前主流程已很少写入，可视为历史表。",
    "confirm": "某次导入生成的确认表 Excel/PDF 路径。",
    "confirmbatch": "按客户（一汽类别如 NB、BJ）保存确认表默认备注。",
    "orderinfo": "解析出的零件级订单行，报表导出使用。",
    "originalunit": "品番量表。无主键。导入时按客户侧删除后全量插入（丰田清 FTEC/FTET，本田清 DHEC/CHAM/GHAC）。重复品番会导致确认表或标签数量异常。",
    "pickupplan": "广本取货计划。量产、SP、八千代、阶段品均写入本表，下载套票主要查询本表。量产按 no 删旧再插，SP 按 p_code 删旧再插。",
    "ord_weekorder": "广本与东风周订单共用。东风页面筛选条件为 r_location like 'D%'。",
    "dhec_dayplan": "东风本田日计划。导入按送货传票号 s_code 删除后插入；新文件中不存在的旧传票行不会自动删除。下载套票按车组、传票号到目录中按文件名查找 PDF，不读路径表。",
    "dhec_summons_order": "东本补给传票拆分时写入，传票号与订单号对照。",
    "cham_ord_week": "佛山本田周订单。出货指示按物料、计划、日期、数量等匹配传票；有订单号的行才会被配到。取消订单须在周订单页删除。",
    "cham_court_summons": "佛山本田单张传票文件路径。",
    "cham_board": "佛山本田看板路径及目录。",
    "chamstacklabel": "佛山本田托标签。shippingCode 为出货指示号。",
    "ftet_chain": "一汽发动机直连配货链数据。生成链标签时按 ordercode、bei 取 l1～l14。",
    "ftet_plan": "一汽发动机集货计划，按日期与订单号记录。",
    "user": "系统用户。",
    "role": "角色。",
    "user_role": "用户与角色对应。",
    "menu": "功能菜单。",
    "role_menu": "角色与菜单对应。无主键。",
}

GROUPS = [
    ("导入与下载", ["orderimport", "ord_inp_detail2", "ord_inp_detail", "confirm", "confirmbatch", "orderinfo"]),
    ("量表", ["originalunit"]),
    ("广州本田 GHAC", ["pickupplan", "ord_weekorder"]),
    ("东风本田 DHEC", ["dhec_dayplan", "dhec_summons_order"]),
    ("佛山本田 CHAM", ["cham_ord_week", "cham_board", "cham_court_summons", "chamstacklabel"]),
    ("一汽发动机 FTET", ["ftet_chain", "ftet_plan"]),
    ("权限", ["user", "role", "user_role", "menu", "role_menu"]),
]


def set_run_font(run, east="宋体", latin="Times New Roman", size=12, bold=False, color=None):
    run.bold = bold
    run.italic = False
    run.font.size = Pt(size)
    run.font.name = latin
    if color is not None:
        run.font.color.rgb = color
    rPr = run._element.get_or_add_rPr()
    rFonts = rPr.find(qn("w:rFonts"))
    if rFonts is None:
        rFonts = OxmlElement("w:rFonts")
        rPr.append(rFonts)
    rFonts.set(qn("w:ascii"), latin)
    rFonts.set(qn("w:hAnsi"), latin)
    rFonts.set(qn("w:eastAsia"), east)
    rFonts.set(qn("w:cs"), latin)


def add_text(paragraph, text, **kwargs):
    run = paragraph.add_run(text)
    set_run_font(run, **kwargs)
    return run


def set_paragraph_format(
    p,
    before=0,
    after=0,
    line=None,
    exact=False,
    first_indent=None,
    align=None,
    keep_next=False,
    outline=None,
):
    pf = p.paragraph_format
    pf.space_before = Pt(before)
    pf.space_after = Pt(after)
    if line is not None:
        if exact:
            pf.line_spacing_rule = WD_LINE_SPACING.EXACTLY
            pf.line_spacing = Pt(line)
        else:
            pf.line_spacing = line
    if first_indent is not None:
        pf.first_line_indent = first_indent
    if align is not None:
        p.alignment = align
    pf.keep_with_next = keep_next
    if outline is not None:
        pPr = p._p.get_or_add_pPr()
        el = pPr.find(qn("w:outlineLvl"))
        if el is None:
            el = OxmlElement("w:outlineLvl")
            pPr.append(el)
        el.set(qn("w:val"), str(outline))


def shade_cell(cell, fill):
    tcPr = cell._tc.get_or_add_tcPr()
    shd = OxmlElement("w:shd")
    shd.set(qn("w:val"), "clear")
    shd.set(qn("w:color"), "auto")
    shd.set(qn("w:fill"), fill)
    tcPr.append(shd)


def set_cell_borders(cell, color="8FA2B5", sz="4"):
    tcPr = cell._tc.get_or_add_tcPr()
    old = tcPr.find(qn("w:tcBorders"))
    if old is not None:
        tcPr.remove(old)
    borders = OxmlElement("w:tcBorders")
    for edge in ("top", "left", "bottom", "right"):
        el = OxmlElement("w:" + edge)
        el.set(qn("w:val"), "single")
        el.set(qn("w:sz"), sz)
        el.set(qn("w:space"), "0")
        el.set(qn("w:color"), color)
        borders.append(el)
    tcPr.append(borders)


def set_row_height(row, cm, rule="atLeast"):
    trPr = row._tr.get_or_add_trPr()
    old = trPr.find(qn("w:trHeight"))
    if old is not None:
        trPr.remove(old)
    el = OxmlElement("w:trHeight")
    el.set(qn("w:val"), str(int(Cm(cm).twips)))
    el.set(qn("w:hRule"), rule)
    trPr.append(el)


def set_repeat_header(row):
    trPr = row._tr.get_or_add_trPr()
    el = OxmlElement("w:tblHeader")
    el.set(qn("w:val"), "true")
    trPr.append(el)


def set_table_layout(table, widths_cm):
    tbl = table._tbl
    tblPr = tbl.tblPr
    tblW = tblPr.find(qn("w:tblW"))
    if tblW is None:
        tblW = OxmlElement("w:tblW")
        tblPr.append(tblW)
    total = int(sum(Cm(w).twips for w in widths_cm))
    tblW.set(qn("w:w"), str(total))
    tblW.set(qn("w:type"), "dxa")

    layout = tblPr.find(qn("w:tblLayout"))
    if layout is None:
        layout = OxmlElement("w:tblLayout")
        tblPr.append(layout)
    layout.set(qn("w:type"), "fixed")

    mar = tblPr.find(qn("w:tblCellMar"))
    if mar is not None:
        tblPr.remove(mar)
    mar = OxmlElement("w:tblCellMar")
    for edge, val in (("top", "40"), ("left", "80"), ("bottom", "40"), ("right", "80")):
        el = OxmlElement("w:" + edge)
        el.set(qn("w:w"), val)
        el.set(qn("w:type"), "dxa")
        mar.append(el)
    tblPr.append(mar)

    grid = tbl.find(qn("w:tblGrid"))
    if grid is not None:
        tbl.remove(grid)
    grid = OxmlElement("w:tblGrid")
    for w in widths_cm:
        gc = OxmlElement("w:gridCol")
        gc.set(qn("w:w"), str(int(Cm(w).twips)))
        grid.append(gc)
    tbl.insert(1, grid)

    for row in table.rows:
        for i, cell in enumerate(row.cells):
            tc = cell._tc
            tcPr = tc.get_or_add_tcPr()
            tcW = tcPr.find(qn("w:tcW"))
            if tcW is None:
                tcW = OxmlElement("w:tcW")
                tcPr.append(tcW)
            tcW.set(qn("w:w"), str(int(Cm(widths_cm[i]).twips)))
            tcW.set(qn("w:type"), "dxa")


def prevent_row_split(row):
    trPr = row._tr.get_or_add_trPr()
    cant = OxmlElement("w:cantSplit")
    trPr.append(cant)


def fill_cell(cell, text, size=10.5, bold=False, align="left", header=False, east="宋体"):
    cell.vertical_alignment = WD_CELL_VERTICAL_ALIGNMENT.CENTER
    p = cell.paragraphs[0]
    for r in list(p.runs):
        r._element.getparent().remove(r._element)
    pf = p.paragraph_format
    pf.space_before = Pt(2)
    pf.space_after = Pt(2)
    pf.line_spacing = 1.0
    pf.first_line_indent = Cm(0)
    if align == "center":
        p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    elif align == "right":
        p.alignment = WD_ALIGN_PARAGRAPH.RIGHT
    else:
        p.alignment = WD_ALIGN_PARAGRAPH.LEFT
    color = RGBColor(0xFF, 0xFF, 0xFF) if header else BODY
    add_text(p, text or "", east=east, size=size, bold=bold, color=color, latin="Times New Roman")


def add_table(doc, headers, rows, widths, center_cols=None, caption=None, caption_no=None):
    center_cols = set(center_cols or [])
    if caption:
        p = doc.add_paragraph()
        set_paragraph_format(
            p, before=6, after=3, line=16, exact=True, first_indent=Cm(0), align=WD_ALIGN_PARAGRAPH.CENTER
        )
        title = f"表 {caption_no}  {caption}" if caption_no else caption
        add_text(p, title, east="黑体", size=10.5, bold=True, color=BLACK)

    table = doc.add_table(rows=1 + len(rows), cols=len(headers))
    table.alignment = WD_TABLE_ALIGNMENT.CENTER
    table.autofit = False
    set_table_layout(table, widths)

    set_row_height(table.rows[0], 0.92)
    set_repeat_header(table.rows[0])
    prevent_row_split(table.rows[0])
    for i, h in enumerate(headers):
        cell = table.rows[0].cells[i]
        shade_cell(cell, HEADER_FILL)
        set_cell_borders(cell, "1F4E79", "4")
        fill_cell(cell, h, size=10.5, bold=True, align="center", header=True, east="黑体")

    for r_idx, row_data in enumerate(rows):
        row = table.rows[r_idx + 1]
        set_row_height(row, 0.78)
        prevent_row_split(row)
        for c_idx in range(len(headers)):
            cell = row.cells[c_idx]
            val = row_data[c_idx] if c_idx < len(row_data) else ""
            if r_idx % 2 == 1:
                shade_cell(cell, ALT_FILL)
            set_cell_borders(cell, "B7C4D1", "4")
            align = "center" if c_idx in center_cols else "left"
            fill_cell(cell, str(val), size=10.5, bold=False, align=align)

    spacer = doc.add_paragraph()
    set_paragraph_format(spacer, before=0, after=6, line=6, exact=True, first_indent=Cm(0))
    return table


def add_body(doc, text, first_indent=True):
    p = doc.add_paragraph()
    set_paragraph_format(
        p,
        before=0,
        after=0,
        line=28,
        exact=True,
        first_indent=Pt(24) if first_indent else Cm(0),
        align=WD_ALIGN_PARAGRAPH.JUSTIFY,
    )
    add_text(p, text, east="宋体", size=12, color=BODY)
    return p


def add_unnumbered_title(doc, text):
    p = doc.add_paragraph()
    add_text(p, text, east="黑体", size=16, bold=True, color=BLACK)
    set_paragraph_format(
        p, before=18, after=12, line=28, exact=True, first_indent=Cm(0), align=WD_ALIGN_PARAGRAPH.CENTER
    )
    return p


def add_heading1(doc, text):
    p = doc.add_heading("", level=1)
    for r in list(p.runs):
        r._element.getparent().remove(r._element)
    add_text(p, text, east="黑体", size=16, bold=True, color=BLACK)
    set_paragraph_format(p, before=18, after=8, line=28, exact=True, first_indent=Cm(0), keep_next=True)
    p.alignment = WD_ALIGN_PARAGRAPH.LEFT
    return p


def add_heading2(doc, text):
    p = doc.add_heading("", level=2)
    for r in list(p.runs):
        r._element.getparent().remove(r._element)
    add_text(p, text, east="黑体", size=14, bold=True, color=BLACK)
    set_paragraph_format(p, before=12, after=6, line=24, exact=True, first_indent=Cm(0), keep_next=True)
    return p


def add_heading3(doc, text):
    p = doc.add_heading("", level=3)
    for r in list(p.runs):
        r._element.getparent().remove(r._element)
    add_text(p, text, east="黑体", size=12, bold=True, color=BLACK)
    set_paragraph_format(p, before=6, after=3, line=22, exact=True, first_indent=Cm(0), keep_next=True)
    return p


def add_list_item(doc, text, ordered=False, number=None):
    p = doc.add_paragraph()
    set_paragraph_format(p, before=0, after=0, line=28, exact=True, first_indent=Pt(24))
    prefix = f"{number}. " if ordered else "（1）"
    add_text(p, f"{prefix}{text}", east="宋体", size=12, color=BODY)
    return p


def add_caption_note(doc, text):
    p = doc.add_paragraph()
    set_paragraph_format(p, before=0, after=6, line=18, exact=True, first_indent=Cm(0))
    add_text(p, text, east="宋体", size=9, color=RGBColor(0x55, 0x55, 0x55))


def add_field_run(paragraph, instr, placeholder="1"):
    r1 = paragraph.add_run()
    b = OxmlElement("w:fldChar")
    b.set(qn("w:fldCharType"), "begin")
    r1._r.append(b)

    r2 = paragraph.add_run()
    it = OxmlElement("w:instrText")
    it.set(qn("xml:space"), "preserve")
    it.text = instr
    r2._r.append(it)

    r3 = paragraph.add_run()
    s = OxmlElement("w:fldChar")
    s.set(qn("w:fldCharType"), "separate")
    r3._r.append(s)

    r4 = paragraph.add_run(placeholder)
    set_run_font(r4, east="宋体", size=9, color=BODY)

    r5 = paragraph.add_run()
    e = OxmlElement("w:fldChar")
    e.set(qn("w:fldCharType"), "end")
    r5._r.append(e)


def _header_border(paragraph, edge="bottom", sz="12"):
    pPr = paragraph._p.get_or_add_pPr()
    pBdr = OxmlElement("w:pBdr")
    el = OxmlElement("w:" + edge)
    el.set(qn("w:val"), "single")
    el.set(qn("w:sz"), sz)
    el.set(qn("w:space"), "4")
    el.set(qn("w:color"), NAVY)
    pBdr.append(el)
    pPr.append(pBdr)


def setup_cover_section(section):
    section.different_first_page_header_footer = False
    for part in (section.header, section.footer):
        part.is_linked_to_previous = False
        p = part.paragraphs[0]
        for r in list(p.runs):
            r._element.getparent().remove(r._element)
    fp = section.footer.paragraphs[0]
    fp.alignment = WD_ALIGN_PARAGRAPH.CENTER
    set_paragraph_format(fp, before=0, after=0, line=16, exact=True, first_indent=Cm(0))
    add_text(fp, "内部资料", east="宋体", size=9, color=BODY)


def setup_body_section(section):
    section.different_first_page_header_footer = False
    sectPr = section._sectPr
    pg = sectPr.find(qn("w:pgNumType"))
    if pg is None:
        pg = OxmlElement("w:pgNumType")
        sectPr.append(pg)
    pg.set(qn("w:start"), "1")

    header = section.header
    header.is_linked_to_previous = False
    hp = header.paragraphs[0]
    for r in list(hp.runs):
        r._element.getparent().remove(r._element)
    set_paragraph_format(hp, before=0, after=0, line=16, exact=True, first_indent=Cm(0))
    hp.alignment = WD_ALIGN_PARAGRAPH.LEFT
    add_text(hp, "南沙电装 OMS 系统", east="宋体", size=9, color=NAVY_RGB)
    hp.paragraph_format.tab_stops.add_tab_stop(CONTENT_WIDTH, WD_TAB_ALIGNMENT.RIGHT)
    add_text(hp, "\t数据库设计说明书", east="宋体", size=9, color=NAVY_RGB)
    _header_border(hp, "bottom", "12")

    footer = section.footer
    footer.is_linked_to_previous = False
    fp = footer.paragraphs[0]
    for r in list(fp.runs):
        r._element.getparent().remove(r._element)
    set_paragraph_format(fp, before=0, after=0, line=16, exact=True, first_indent=Cm(0))
    fp.alignment = WD_ALIGN_PARAGRAPH.CENTER
    _header_border(fp, "top", "6")
    add_text(fp, "第 ", east="宋体", size=9, color=BODY)
    add_field_run(fp, " PAGE ", "1")
    add_text(fp, " 页    共 ", east="宋体", size=9, color=BODY)
    add_field_run(fp, " SECTIONPAGES ", "1")
    add_text(fp, " 页", east="宋体", size=9, color=BODY)


def enable_update_fields(doc):
    settings = doc.settings.element
    el = OxmlElement("w:updateFields")
    el.set(qn("w:val"), "true")
    settings.append(el)


def parse_sql(sql_text: str):
    tables = {}
    parts = re.split(r"CREATE TABLE `([^`]+)`\s*\(", sql_text)
    # parts: [preamble, name1, body1+tail, name2, body2+tail, ...]
    for i in range(1, len(parts), 2):
        name = parts[i]
        rest = parts[i + 1]
        body, tail = rest.split(") ENGINE", 1)
        engine_m = re.match(r"\s*=\s*(\w+)", tail)
        engine = engine_m.group(1) if engine_m else ""
        charset_m = re.search(r"CHARACTER SET\s*=\s*(\w+)", tail)
        collate_m = re.search(r"COLLATE\s*=\s*(\S+)", tail)
        comment_m = re.search(r"COMMENT\s*=\s*'((?:\\'|[^'])*)'", tail)
        table_comment = (comment_m.group(1) if comment_m else "").replace("\\r\\n", "").replace("\r", "").replace("\n", "").strip()

        pk_cols = []
        indexes = []
        fields = []
        for raw_line in body.split("\n"):
            line = raw_line.strip().rstrip(",")
            if not line:
                continue
            pk = re.match(r"PRIMARY KEY\s*\((.+)\)", line, re.I)
            if pk:
                pk_cols = [c.strip().strip("`") for c in re.findall(r"`([^`]+)`", pk.group(1))]
                continue
            idx = re.match(r"INDEX\s+`([^`]+)`\s*\((.+)\)", line, re.I)
            if idx:
                cols = [c.strip().strip("`") for c in re.findall(r"`([^`]+)`", idx.group(2))]
                indexes.append((idx.group(1), cols))
                continue
            fm = re.match(r"`([^`]+)`\s+([a-zA-Z]+(?:\s*\([^)]+\))?)\s*(.*)$", line)
            if not fm:
                continue
            fname, ftype, after = fm.group(1), fm.group(2).replace(" ", ""), fm.group(3)
            nullable = "否" if re.search(r"\bNOT NULL\b", after, re.I) else "是"
            ai = bool(re.search(r"AUTO_INCREMENT", after, re.I))
            cm = re.search(r"COMMENT\s+'((?:\\'|[^'])*)'", after)
            comment = cm.group(1).replace("\\r\\n", "").strip() if cm else ""
            fields.append(
                {
                    "name": fname,
                    "type": ftype,
                    "nullable": nullable,
                    "ai": ai,
                    "comment": comment,
                }
            )

        pk_set = set(pk_cols)
        idx_map = {}
        for iname, cols in indexes:
            for c in cols:
                idx_map.setdefault(c, []).append(iname)

        tables[name] = {
            "name": name,
            "engine": engine,
            "charset": charset_m.group(1) if charset_m else "",
            "collate": collate_m.group(1) if collate_m else "",
            "comment": table_comment,
            "pk": pk_cols,
            "indexes": indexes,
            "fields": fields,
            "idx_map": idx_map,
            "pk_set": pk_set,
        }
    return tables


def field_constraint(table, field):
    bits = []
    if field["name"] in table["pk_set"]:
        bits.append("主键")
    if field["ai"]:
        bits.append("自增")
    if field["name"] in table["idx_map"]:
        bits.append("索引")
    return "、".join(bits) if bits else "—"


def field_remark(table_name, field):
    comment = field["comment"].strip()
    extra = EXTRA_FIELD.get((table_name, field["name"]), "")
    if comment and extra:
        if comment[-1] not in "。；;":
            comment += "。"
        return comment + extra
    return extra or comment or "—"


def simplify_type(t):
    return t.replace("CHARACTER", "").strip()


def build_cover(doc):
    # 密级
    p = doc.add_paragraph()
    set_paragraph_format(p, before=0, after=0, line=22, exact=True, first_indent=Cm(0), align=WD_ALIGN_PARAGRAPH.RIGHT)
    add_text(p, "密　　级：内部", east="黑体", size=12, bold=True, color=BLACK)

    p = doc.add_paragraph()
    set_paragraph_format(p, before=48, after=0, line=28, exact=True, first_indent=Cm(0), align=WD_ALIGN_PARAGRAPH.CENTER)
    add_text(p, "南沙电装 OMS 系统", east="黑体", size=22, bold=True, color=NAVY_RGB)

    p = doc.add_paragraph()
    set_paragraph_format(p, before=18, after=0, line=40, exact=True, first_indent=Cm(0), align=WD_ALIGN_PARAGRAPH.CENTER)
    add_text(p, "数据库设计说明书", east="黑体", size=28, bold=True, color=BLACK)

    p = doc.add_paragraph()
    set_paragraph_format(p, before=8, after=0, line=22, exact=True, first_indent=Cm(0), align=WD_ALIGN_PARAGRAPH.CENTER)
    add_text(p, "Database Design Specification", latin="Times New Roman", east="Times New Roman", size=14, color=NAVY_RGB)

    # decorative line
    line_p = doc.add_paragraph()
    set_paragraph_format(line_p, before=12, after=18, line=6, exact=True, first_indent=Cm(0), align=WD_ALIGN_PARAGRAPH.CENTER)
    pPr = line_p._p.get_or_add_pPr()
    pBdr = OxmlElement("w:pBdr")
    bottom = OxmlElement("w:bottom")
    bottom.set(qn("w:val"), "single")
    bottom.set(qn("w:sz"), "18")
    bottom.set(qn("w:space"), "1")
    bottom.set(qn("w:color"), NAVY)
    pBdr.append(bottom)
    pPr.append(pBdr)

    meta = [
        ("文档编号", "OMS-DB-SPEC-2026-001"),
        ("版　　本", "V1.0"),
        ("数据库名", "omsdb"),
        ("编制日期", "2026年9月3日"),
        ("结构来源", "omsdb.sql（MySQL 8.0.42 结构导出）"),
    ]
    for k, v in meta:
        p = doc.add_paragraph()
        set_paragraph_format(p, before=0, after=0, line=28, exact=True, first_indent=Cm(0), align=WD_ALIGN_PARAGRAPH.CENTER)
        add_text(p, f"{k}：{v}", east="宋体", size=12, color=BODY)

    p = doc.add_paragraph()
    set_paragraph_format(p, before=36, after=6, line=22, exact=True, first_indent=Cm(0), align=WD_ALIGN_PARAGRAPH.CENTER)
    add_text(p, "签署栏", east="黑体", size=12, bold=True, color=BLACK)

    add_table(
        doc,
        ["项　　目", "姓　　名", "日　　期", "签　　名"],
        [
            ["编　　制", "", "2026年9月3日", ""],
            ["审　　核", "", "", ""],
            ["批　　准", "", "", ""],
        ],
        [3.5, 4.0, 4.0, 4.0],
        center_cols={0, 1, 2, 3},
    )


def build_toc(doc):
    add_unnumbered_title(doc, "目　　录")
    p = doc.add_paragraph()
    set_paragraph_format(p, before=6, after=6, line=22, exact=True, first_indent=Cm(0))
    add_text(p, "打开文档后如页码未刷新，请全选后按 F9 更新域。", east="宋体", size=9, color=RGBColor(0x66, 0x66, 0x66))
    p = doc.add_paragraph()
    set_paragraph_format(p, before=0, after=0, line=28, exact=True, first_indent=Cm(0))
    add_field_run(p, r' TOC \o "1-3" \h \z \u ', "（目录域，打开 Word 后自动生成）")
    doc.add_page_break()


def add_table_section(doc, tables, name, caption_seq, h_level=3):
    t = tables[name]
    title = name
    if t["comment"]:
        title = f"{name}（{t['comment']}）"
    if h_level == 2:
        add_heading2(doc, title)
    else:
        add_heading3(doc, title)

    intro = TABLE_INTRO.get(name)
    if intro:
        add_body(doc, intro)

    meta_bits = []
    if t["engine"]:
        meta_bits.append(f"存储引擎 {t['engine']}")
    if t["charset"]:
        meta_bits.append(f"字符集 {t['charset']}")
    if t["pk"]:
        meta_bits.append("主键 " + "、".join(t["pk"]))
    else:
        meta_bits.append("无主键")
    if t["indexes"]:
        meta_bits.append("索引 " + "、".join(i[0] for i in t["indexes"]))
    add_body(doc, "表属性：" + "；".join(meta_bits) + "。", first_indent=True)

    rows = []
    for i, f in enumerate(t["fields"], 1):
        rows.append(
            [
                str(i),
                f["name"],
                f["type"],
                f["nullable"],
                field_constraint(t, f),
                field_remark(name, f),
            ]
        )
    add_table(
        doc,
        ["序号", "字段名", "数据类型", "允许空", "约束", "说明"],
        rows,
        [1.15, 3.15, 2.7, 1.35, 1.5, 5.65],
        center_cols={0, 3, 4},
        caption=f"{name} 字段定义",
        caption_no=caption_seq[0],
    )
    caption_seq[0] += 1


def build(sql_path: Path, out_path: Path):
    tables = parse_sql(sql_path.read_text(encoding="utf-8"))
    doc = Document()

    section = doc.sections[0]
    section.page_width = Cm(21.0)
    section.page_height = Cm(29.7)
    section.top_margin = Cm(2.54)
    section.bottom_margin = Cm(2.54)
    section.left_margin = Cm(3.17)
    section.right_margin = Cm(2.54)
    section.header_distance = Cm(1.5)
    section.footer_distance = Cm(1.75)
    setup_cover_section(section)
    enable_update_fields(doc)

    styles = doc.styles
    normal = styles["Normal"]
    normal.font.name = "Times New Roman"
    normal.font.size = Pt(12)
    normal.font.color.rgb = BODY
    normal._element.rPr.rFonts.set(qn("w:eastAsia"), "宋体")
    nf = normal.paragraph_format
    nf.line_spacing_rule = WD_LINE_SPACING.EXACTLY
    nf.line_spacing = Pt(28)
    nf.space_before = Pt(0)
    nf.space_after = Pt(0)

    core = doc.core_properties
    core.title = "南沙电装 OMS 系统数据库设计说明书"
    core.subject = "omsdb"
    core.author = "南沙电装 OMS"
    core.comments = "依据 omsdb.sql 结构导出编制，V1.0"

    seq = [1]
    build_cover(doc)

    body_section = doc.add_section(WD_SECTION.NEW_PAGE)
    body_section.page_width = Cm(21.0)
    body_section.page_height = Cm(29.7)
    body_section.top_margin = Cm(2.54)
    body_section.bottom_margin = Cm(2.54)
    body_section.left_margin = Cm(3.17)
    body_section.right_margin = Cm(2.54)
    body_section.header_distance = Cm(1.5)
    body_section.footer_distance = Cm(1.75)
    setup_body_section(body_section)

    add_unnumbered_title(doc, "修订记录")
    add_body(doc, "本文档随数据库结构变更同步修订。历次修订记录如下。")
    add_table(
        doc,
        ["版本", "日期", "修订说明", "编制"],
        [["V1.0", "2026-09-03", "初稿。依据 MySQL 8.0.42 库 omsdb 结构脚本编制，并补充程序中的实际用途。", ""]],
        [2.0, 2.6, 8.4, 2.5],
        center_cols={0, 1, 3},
        caption="修订记录",
        caption_no=seq[0],
    )
    seq[0] += 1
    doc.add_page_break()
    build_toc(doc)

    add_heading1(doc, "1  概述")
    add_heading2(doc, "1.1  编写目的")
    add_body(
        doc,
        "本说明书给出南沙电装 OMS（订单与物流单据处理系统）数据库 omsdb 的表结构、字段含义、逻辑关系及维护约定，供开发、测试与运维查阅。字段说明以库内 COMMENT 为准；库注释缺失或与程序不一致处，在说明列中另行注明。",
    )
    add_heading2(doc, "1.2  适用范围")
    add_body(
        doc,
        "适用于 OMS 后台服务、前端功能及与主机厂单据解析、套票下载、确认表生成相关的数据存储。客户范围包括：FTET 一汽发动机、FTCE/FTEC 长春、DHEC 东风本田、CHAM 佛山本田、GHAC 广州本田。",
    )
    add_heading2(doc, "1.3  引用文件")
    add_body(doc, "omsdb.sql：Navicat 结构导出，MySQL 8.0.42，导出日期 2026年9月3日。导出内容为表结构，不含业务数据。")
    add_body(doc, "OMS 应用程序及配置文件 Web.config（其中 fileUpload 为解析输出根目录）。")

    add_heading1(doc, "2  运行环境与约定")
    add_heading2(doc, "2.1  库环境")
    add_table(
        doc,
        ["项目", "内容"],
        [
            ["数据库管理系统", "MySQL 8.0.42"],
            ["模式（Schema）", "omsdb"],
            ["表数量", f"{len(tables)}"],
            ["默认字符集", "utf8mb4（表级）；部分字段仍为 utf8mb3"],
            ["外键", "无。表间通过业务字段关联"],
            ["视图 / 存储过程", "结构脚本中未包含"],
        ],
        [4.5, 11.0],
        center_cols={0},
        caption="数据库环境",
        caption_no=seq[0],
    )
    seq[0] += 1

    add_heading2(doc, "2.2  设计约定")
    add_body(doc, "库内未建立 FOREIGN KEY。主从关系由程序维护，典型关联为 orderimport.id 对应 ord_inp_detail2.mainId、confirm.mainId。")
    add_body(doc, "路径类字段保存 Windows 磁盘绝对路径，不保存相对路径。应用或数据迁移换盘时，须同时修改 Web.config 中 fileUpload 及库内路径字段。")
    add_body(doc, "部分表无主键（originalunit、role_menu）。导入量表时按客户删除后全量插入，须避免重复品番。")
    add_body(doc, "ord_inp_detail 与 ord_inp_detail2 并存。现行解析与下载只维护 ord_inp_detail2。")

    add_heading1(doc, "3  逻辑数据模型")
    add_heading2(doc, "3.1  权限")
    add_body(doc, "用户、角色、菜单通过中间表关联：user → user_role → role → role_menu → menu。程序按用户显示名 name 加载菜单，不以用户数字主键为查询条件。")
    add_heading2(doc, "3.2  导入主链路")
    add_body(
        doc,
        "上传文件写入 orderimport。解析成功后，生成文件路径写入 ord_inp_detail2（mainId 指向导入主键），确认表路径写入 confirm。零件级汇总可写入 orderinfo。量表 originalunit 按品番、客户简称被各客户解析与确认表引用。",
    )
    add_heading2(doc, "3.3  分客户业务表")
    add_body(doc, "广州本田以 pickupplan 为取货计划主表，周订单使用 ord_weekorder（与东风共用）。东风本田日计划为 dhec_dayplan，补给传票对照为 dhec_summons_order。")
    add_body(doc, "佛山本田周订单为 cham_ord_week，传票、看板、托标签分别对应 cham_court_summons、cham_board、chamstacklabel。一汽发动机链数据为 ftet_chain，集货计划为 ftet_plan。")

    add_heading1(doc, "4  数据表一览")
    add_body(doc, "下列按业务分组列出全部数据表。中文名取自表 COMMENT，无注释时按程序用途填写。")
    overview_rows = []
    n = 1
    for group, names in GROUPS:
        for name in names:
            t = tables[name]
            overview_rows.append(
                [
                    str(n),
                    group,
                    name,
                    t["comment"] or "—",
                    "、".join(t["pk"]) if t["pk"] else "无",
                    str(len(t["fields"])),
                ]
            )
            n += 1
    add_table(
        doc,
        ["序号", "分组", "表名", "中文名", "主键", "字段数"],
        overview_rows,
        [1.2, 3.3, 3.4, 4.0, 2.0, 1.6],
        center_cols={0, 5},
        caption="数据表一览",
        caption_no=seq[0],
    )
    seq[0] += 1

    chapter_tables = [
        ("5  导入与下载相关表", ["orderimport", "ord_inp_detail2", "ord_inp_detail", "confirm", "confirmbatch", "orderinfo"]),
        ("6  原单位表", ["originalunit"]),
        ("7  广州本田（GHAC）", ["pickupplan", "ord_weekorder"]),
        ("8  东风本田（DHEC）", ["dhec_dayplan", "dhec_summons_order"]),
        ("9  佛山本田（CHAM）", ["cham_ord_week", "cham_board", "cham_court_summons", "chamstacklabel"]),
        ("10  一汽发动机（FTET）", ["ftet_chain", "ftet_plan"]),
        ("11  权限管理", ["user", "role", "user_role", "menu", "role_menu"]),
    ]
    for chapter, names in chapter_tables:
        add_heading1(doc, chapter)
        for name in names:
            add_table_section(doc, tables, name, seq)

    add_heading1(doc, "12  磁盘路径字段")
    add_body(doc, "下列字段保存磁盘绝对路径。服务器换盘、目录迁移或修改 fileUpload 配置时，须同步更新，否则解析、下载与确认表打开将失败。")
    path_rows = [
        ["1", "orderimport", "redundancy1", "原始上传文件"],
        ["2", "ord_inp_detail2", "Stacklabel", "解析生成文件（现行）"],
        ["3", "ord_inp_detail", "Stacklabel、A4Board、Board、comfirm、Receipt、bale、lian", "历史明细路径列"],
        ["4", "confirm", "excelPath、pdfPath", "确认表 Excel / PDF"],
        ["5", "cham_board", "boardPath、boardDictory", "佛山看板文件与目录"],
        ["6", "cham_court_summons", "path", "佛山传票文件"],
        ["7", "chamstacklabel", "inputPath", "佛山托标签文件"],
        ["8", "ftet_chain", "path", "一汽链数据相关文件"],
    ]
    add_table(
        doc,
        ["序号", "表名", "字段", "用途"],
        path_rows,
        [1.2, 3.4, 6.6, 4.3],
        center_cols={0},
        caption="磁盘路径字段清单",
        caption_no=seq[0],
    )
    seq[0] += 1
    add_body(doc, "建议使用 SQL REPLACE 按旧盘符或旧根目录批量替换，替换后抽查各客户最近一笔导入的下载与确认表打开是否正常。")

    add_heading1(doc, "13  维护说明")
    notes = [
        "删除 orderimport 时，应用程序会同时删除对应的 ord_inp_detail2。若仅在数据库中删除主表，将留下路径孤儿行。",
        "originalunit 无主键。重复品番会导致确认表数量、标签张数异常，导入量表后应检查同一客户下品番是否唯一。",
        "orderimport.handlestate 以程序为准：1 解析中，2 成功，3 失败，4 重新解析。",
        "广本 SP 计划按 p_code 覆盖导入，新文件中取消的行不会自动删除。东本日计划按 s_code 覆盖，情形相同。",
        "佛山取消订单须在周订单（cham_ord_week）中删除。仅删出货指示或文件，仍可能按物料与计划匹配到旧订单。",
        "字段 hassupplies、filehash、createPerson 等在库内无 COMMENT，说明列已按程序补全。",
        "字符集混用 utf8mb3 与 utf8mb4。路径或中文文件名出现乱码时，优先检查仍为 utf8mb3 的列。",
    ]
    for i, text in enumerate(notes, 1):
        add_list_item(doc, text, ordered=True, number=i)

    p = doc.add_paragraph()
    set_paragraph_format(p, before=24, after=0, line=28, exact=True, first_indent=Cm(0), align=WD_ALIGN_PARAGRAPH.CENTER)
    add_text(p, "— 全文完 —", east="宋体", size=12, color=BODY)

    out_path.parent.mkdir(parents=True, exist_ok=True)
    doc.save(str(out_path))
    print("wrote", out_path)
    print("tables", len(tables), "captions", seq[0] - 1)


if __name__ == "__main__":
    docs = Path(__file__).resolve().parent
    sql = Path(r"e:\omsdb.sql")
    out = docs / "南沙电装OMS-数据库说明.docx"
    build(sql, out)
