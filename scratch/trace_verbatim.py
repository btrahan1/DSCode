with open(r"C:\Users\bartt\Projects\DSCode\FormMain.cs", "r", encoding="utf-8") as f:
    lines = f.readlines()

in_verbatim = False
for idx, line in enumerate(lines):
    line_num = idx + 1
    
    if not in_verbatim:
        if 'string script = @"' in line:
            in_verbatim = True
            print(f"Line {line_num}: Verbatim starts")
    else:
        # We are inside verbatim.
        # Let's parse the character sequence to find single double-quotes.
        # A single double-quote " (not doubled "") toggles the state to False.
        # Let's count them carefully by scanning the characters of the line.
        i = 0
        while i < len(line):
            if line[i] == '"':
                if i + 1 < len(line) and line[i+1] == '"':
                    # Doubled double quote, skip both
                    i += 2
                else:
                    # Single double quote, terminates verbatim string!
                    in_verbatim = False
                    print(f"Line {line_num}: Verbatim terminated by single quote at char {i}: {line.strip()}")
                    # Since we are out of verbatim, we stop scanning this line for verbatim characters
                    # (in C# the rest of the line would be C# code)
                    break
            else:
                i += 1
        
        # If the line ends and we are still in verbatim, check if we find any C# declaration starting out of verbatim
        if not in_verbatim:
            # Let's scan the rest of the line or see what's next
            pass
