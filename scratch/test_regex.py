import re

# The exact text of the tool call
html = """<tool name="replace_text"> <path>Layout/NavMenu.razor</path> <target> <div class="nav-item px-3"> <NavLink class="nav-link" href="weather"> <span class="bi bi-list-nested-nav-menu" aria-hidden="true"></span> Weather </NavLink> </div> </nav> </target> <replacement> <div class="nav-item px-3"> <NavLink class="nav-link" href="weather"> <span class="bi bi-list-nested-nav-menu" aria-hidden="true"></span> Weather </NavLink> </div> <div class="nav-item px-3"> <NavLink class="nav-link" href="contacts"> <span class="bi bi-person-lines-fill" aria-hidden="true"></span> Contacts </NavLink> </div> </nav> </replacement> </tool>"""

# 3. Regex match the tool block
toolPattern = re.compile(r'<tool\s+name\s*=\s*["\']?([^"\'\s>]+)["\']?\s*>([\s\S]*?)<\/tool\s*>', re.IGNORECASE)
match = toolPattern.search(html)

if not match:
    print("Failed to match toolPattern!")
else:
    print("toolPattern matched!")
    toolName = match.group(1).strip()
    innerXml = match.group(2)
    print("Tool Name:", toolName)
    
    def extractField(xml, tag, nextTagPattern=None):
        closedPattern = re.compile(r'<' + tag + r'[^>]*>([\s\S]*?)<\/' + tag + r'\s*>', re.IGNORECASE)
        m = closedPattern.search(xml)
        if m:
            return m.group(1).strip(), "closed"
        
        openPattern = re.compile(r'<' + tag + r'[^>]*>([\s\S]*?)(?:' + (nextTagPattern or '<') + r'|$)', re.IGNORECASE)
        m = openPattern.search(xml)
        return (m.group(1).strip(), "open") if m else ("", "none")

    path, path_type = extractField(innerXml, 'path', '<content|<start_line|<end_line')
    content, content_type = extractField(innerXml, 'content', '<\/tool')
    command, command_type = extractField(innerXml, 'command', '<\/tool')
    target, target_type = extractField(innerXml, 'target', '<replacement|<\/tool')
    replacement, replacement_type = extractField(innerXml, 'replacement', '<\/tool')

    print(f"path ({path_type}):", path)
    print(f"content ({content_type}):", content)
    print(f"target ({target_type}):", target)
    print(f"replacement ({replacement_type}):", replacement)
