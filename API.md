## POST https://up.woozooo.com/doupload.php

### 请求
|参数|可选|可选值|
|:-:|:-:|:-:|
|task||2：创建目录<br/>3：删除目录<br/>5：文件列表<br/>6：删除文件<br/>18：目录信息<br/>22：外链ID <br />47：目录列表|
|file_id|√|文件ID，task=22 或 task=6|
|uid|√|用户ID，task=5 或 task=47|
|pg|√|目录ID，根目录为-1，task=5|
|folder_id|√|上级目录ID，根目录为-1，task=5 或 task=47 或 task=3|
|pg|√|页码，task=5|
|vei|√|task=5 或 task=47|

### 响应

> 获取目录列表

```json
{
  "zt": 1,
  "info": [],
  "text": [
    {
      "onof": "1",
      "folderlock": "0",
      "is_lock": "0",
      "is_copyright": "0",
      "name": "Folder0",
      "fol_id": "12345678",
      "folder_des": ""
    },
    {
      "onof": "1",
      "folderlock": "0",
      "is_lock": "0",
      "is_copyright": "0",
      "name": "Folder1",
      "fol_id": "23456789",
      "folder_des": ""
    }
  ],
  "dat": null
}

```


> 获取文件列表

```json
{
  "zt": 1,
  "info": 1,
  "text": [
    {
      "icon": "zip",
      "id": "231922787",
      "name_all": "file0.zip",
      "name": "file0.zip",
      "size": "688.0 B",
      "time": "3 小时前",
      "downs": "0",
      "onof": "0",
      "is_lock": "0",
      "filelock": "0",
      "is_copyright": 0,
      "is_bakdownload": 0,
      "bakdownload": "0",
      "is_des": 0,
      "is_ico": 0
    },
    {
      "icon": "zip",
      "id": "231922786",
      "name_all": "file1.zip",
      "name": "file1.zip",
      "size": "809.0 B",
      "time": "3 小时前",
      "downs": "0",
      "onof": "0",
      "is_lock": "0",
      "filelock": "0",
      "is_copyright": 0,
      "is_bakdownload": 0,
      "bakdownload": "0",
      "is_des": 0,
      "is_ico": 0
    }
  ],
  "dat": null
}

```

> 获取文件外联

```json
{
  "zt": 1,
  "info": {
    "pwd": "xxxx",
    "onof": "0",
    "f_id": "xxxxxxxxxx",
    "taoc": "",
    "is_newd": "https://wwqp.lanzouw.com"
  },
  "text": null,
  "dat": null
}
```

> 删除文件

```json
{
  "zt": 1,
  "info": "已删除",
  "text": null,
  "dat": null
}
```

> 删除目录

```json
{
  "zt": 1,
  "info": "删除成功",
  "text": null,
  "dat": null
}
```

> 目录信息

```json
{
  "zt": 1,
  "info": {
    "name": "Folder0",
    "des": "",
    "pwd": "d875",
    "onof": "1",
    "taoc": "",
    "is_newd": "https://wwqp.lanzouw.com",
    "new_url": "https://wwqp.lanzouw.com/b00zxo9hmh"
  },
  "text": null,
  "dat": null
}
```