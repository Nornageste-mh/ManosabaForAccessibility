import io

p = r'D:\DSHWorkSpace\manosaba_a11y\mod\src\ManosabaA11y\MapChoice.cs'
s = io.open(p, encoding='utf-8-sig').read()

old = '''            sb.Append("地图上可前往 ").Append(_entries.Count).Append(" 个地点");
            if (!string.IsNullOrEmpty(curFloor)) sb.Append("（").Append(curFloor).Append("）");
            sb.Append("：");
            // 只有确实跨楼层分组时才念「在XF：」（同层时那句是废话）
            bool multiFloor = false;
            for (int i = 1; i < _entries.Count; i++) if (_entries[i].Floor != _entries[i - 1].Floor) { multiFloor = true; break; }'''
new = '''            // 只有确实跨楼层分组时才念「在XF：」（同层时那句是废话）
            bool multiFloor = false;
            for (int i = 1; i < _entries.Count; i++) if (_entries[i].Floor != _entries[i - 1].Floor) { multiFloor = true; break; }
            sb.Append("地图上可前往 ").Append(_entries.Count).Append(" 个地点");
            // 跨层时不再提前念「（1F）」——后面每个分组已经带了楼层，重复念是噪音。
            if (!multiFloor && !string.IsNullOrEmpty(curFloor)) sb.Append("（").Append(curFloor).Append("）");
            sb.Append("：");'''
if old not in s:
    raise SystemExit('announce block not found')
s = s.replace(old, new, 1)
io.open(p, 'w', encoding='utf-8-sig', newline='\n').write(s)
print('announce text polished')
