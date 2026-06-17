# -*- coding: utf-8 -*-
"""從 .md 的 # / ## 標題為 PDF 加入書籤（pypdf）。"""
import os
import re
import sys
import unicodedata

from pypdf import PdfReader, PdfWriter

sys.stdout.reconfigure(encoding="utf-8")


def norm(s):
    return re.sub(r"\s+", "", unicodedata.normalize("NFKC", s or ""))


def main():
    md_path, pdf_path = sys.argv[1], sys.argv[2]
    headings = []
    in_fence = False
    with open(md_path, encoding="utf-8") as f:
        for line in f:
            st = line.strip()
            if st.startswith("```") or st.startswith("~~~"):
                in_fence = not in_fence
                continue
            if in_fence:
                continue
            m = re.match(r"^(#{1,2})\s+(.+?)\s*$", line.rstrip("\n"))
            if m:
                headings.append((len(m.group(1)), m.group(2).strip()))

    reader = PdfReader(pdf_path)
    pages = [norm(p.extract_text()) for p in reader.pages]
    keys = [norm(t) for _, t in headings]
    toc_set = {i for i, t in enumerate(pages) if sum(1 for k in keys if k and k in t) >= 8}

    def find_page(title):
        key = norm(title)
        for i, t in enumerate(pages):
            if i in toc_set:
                continue
            if key and key in t:
                return i
        return None

    writer = PdfWriter()
    writer.append(reader)
    writer.add_outline_item("封面", 0, parent=None)
    current = None
    last_pg = 0
    for level, title in headings:
        if norm(title) == "目錄":
            continue
        pg = find_page(title)
        if pg is None:
            pg = last_pg
        else:
            last_pg = pg
        if level == 1:
            current = writer.add_outline_item(title, pg, parent=None)
        else:
            writer.add_outline_item(title, pg, parent=current)

    try:
        writer.set_page_mode("/UseOutlines")
    except Exception:
        pass

    tmp = pdf_path + ".tmp"
    with open(tmp, "wb") as f:
        writer.write(f)
    os.replace(tmp, pdf_path)
    print(f"{pdf_path}: {len(headings)} bookmarks, {len(reader.pages)} pages")


if __name__ == "__main__":
    main()
