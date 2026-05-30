import re

test_cases = [
    '<tool name="list_directory"></tool>',
    '<tool name="list_directory" />',
    '<tool name="list_directory"/>',
    '<tool name="list_directory">  </tool>',
    '<tool name="read_file"><path>foo.txt</path></tool>'
]

toolPattern = re.compile(
    r'<tool\s+name\s*=\s*["\']?([^"\'\s>]+)["\']?\s*(?:\/>|>\s*<\/tool\s*>|>([\s\S]*?)<\/tool\s*>)',
    re.IGNORECASE
)

for idx, tc in enumerate(test_cases):
    match = toolPattern.search(tc)
    if match:
        name = match.group(1)
        content = match.group(2) or ""
        print(f"Test {idx+1} Match! Name: '{name}', Content: '{content.strip()}'")
    else:
        print(f"Test {idx+1} Failed to match!")
