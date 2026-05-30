with open(r"C:\Users\bartt\Projects\DSCode\FormMain.cs", "r", encoding="utf-8") as f:
    lines = f.readlines()

in_verbatim = False
start_marker = ""
for idx, line in enumerate(lines):
    line_num = idx + 1
    
    if not in_verbatim:
        if '@"' in line and '=' in line:
            in_verbatim = True
            start_marker = '@"'
            print(f"Line {line_num}: Verbatim starts (marker: {line.strip()})")
    else:
        # Scan for single double-quotes
        i = 0
        while i < len(line):
            if line[i] == '"':
                if i + 1 < len(line) and line[i+1] == '"':
                    i += 2
                else:
                    in_verbatim = False
                    print(f"Line {line_num}: Verbatim terminated by single quote at char {i}: {line.strip()}")
                    break
            else:
                i += 1
