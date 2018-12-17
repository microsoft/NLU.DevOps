import glob
import re
import sys

def extract_results(files):
    total = 0
    failed = 0
    for filepath in glob.glob(files, recursive=True):
        file_content = ""
        with open(filepath, 'r') as f:
            file_content = f.read()
        
        # Search for test statistics in either VSTest or NUnit format 
        match = re.search(r'<Counters total="(\d+)" executed="\d+" passed="\d+" failed="(\d+)"', file_content)
        if not match:
            match = re.search(r'<test-suite type="Assembly" .*? total="(\d+)" passed="\d+" failed="(\d+)"', file_content)
        
        if not match:
            raise Exception("Could not find test results in " + filepath)    
        total += float(match.group(1))
        failed += float(match.group(2))
    return failed / total

def assert_results(master_files, current_files, delta):
    master_results = extract_results(master_files)
    current_results = extract_results(current_files)
    if current_results > (master_results + delta):
        raise Exception("Model performance regressed.")

if len(sys.argv) < 3:
    raise Exception("Usage: compare_results.py <masterResultsPath> <currentResultsPath> [delta]")

delta = float(sys.argv[3]) if len(sys.argv) > 3 else 0
assert_results(sys.argv[1], sys.argv[2], delta)
