with open(r"C:\Users\bartt\Projects\DSCode\FormMain.cs", "r", encoding="utf-8") as f:
    lines = f.readlines()

in_verbatim = False
quote_count = 0
for idx, line in enumerate(lines):
    line_num = idx + 1
    # We look for the declaration of string script = @"
    if 'string script = @"' in line:
        in_verbatim = True
        print(f"Verbatim string starts at line {line_num}")
        continue
    
    if in_verbatim:
        # Count quotes on this line. 
        # In a C# verbatim string, any double quote is either "" (escaped) or a single " (which terminates the string).
        # Let's count how many double quotes are on this line.
        # If it's odd, it means there is an unmatched double quote which terminates the string!
        quotes = line.count('"')
        if quotes > 0:
            print(f"Line {line_num}: contains {quotes} double quotes: {line.strip()}")
        # Wait, a single quote at the end of the script terminates it.
        # The script ends with })();"; which contains a double quote followed by semicolon.
        # Let's see if })();"; is in the line
        if '})();";' in line:
            in_verbatim = False
            print(f"Verbatim string ends at line {line_num}")
            break
