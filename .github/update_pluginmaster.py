import json
import os
import sys
import time

repo_path = sys.argv[1]
version = sys.argv[2]
repo_full_name = sys.argv[3]

json_path = os.path.join(repo_path, 'pluginmaster.json')

with open(json_path, 'r', encoding='utf-8') as f:
    data = json.load(f)

download_url = f"https://github.com/{repo_full_name}/releases/download/v{version}/latest.zip"

entry = next((x for x in data if x.get('InternalName') == 'UntarnishedHeart'), {})

entry['Name'] = 'Untarnished Heart'
entry['Author'] = 'AtmoOmen, QianChangUwU（魔改）'
entry['Punchline'] = '自动化副本完成、奖励循环刷取插件'
entry['Description'] = '原本是自动化防护职业副本场次成就刷取工具\n注: 副本战斗需要自行使用其他插件解决, 硬要用本插件写也不是不行'
entry['InternalName'] = 'UntarnishedHeart'
entry['AssemblyVersion'] = version
entry['TestingAssemblyVersion'] = version
entry['DalamudApiLevel'] = 15
entry['TestingDalamudApiLevel'] = 15
entry['DownloadLinkInstall'] = download_url
entry['DownloadLinkUpdate'] = download_url
entry['DownloadLinkTesting'] = download_url
entry['RepoUrl'] = f'https://github.com/{repo_full_name}'
entry['IconUrl'] = 'https://raw.githubusercontent.com/AtmoOmen/UntarnishedHeart/main/Resources/icon.png'
entry['Tags'] = ['自动', 'T 职', '坦克', '成就', '心之长明', '副本', 'UTH']
entry['ApplicableVersion'] = 'any'
entry['LoadPriority'] = 0
entry['AcceptsFeedback'] = True
entry['LastUpdate'] = int(time.time())

if entry not in data:
    data.append(entry)

with open(json_path, 'w', encoding='utf-8') as f:
    json.dump(data, f, indent=4, ensure_ascii=False)
