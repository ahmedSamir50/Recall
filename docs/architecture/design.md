Status: in-code

## The four projects

```mermaid
flowchart TB
  Maui[Recall.Maui executable]
  UI[Recall.UI screens]
  Adapters[Recall.Adapters libraries]
  Core[Recall.Core rules]
  Maui --> UI
  Maui --> Adapters
  Maui --> Core
  UI --> Core
  Adapters --> Core
```

Recall.Core owns a rule. Recall.UI starts a step and does not choose the rule. Recall.Adapters performs the library step, llama.cpp or ZVec. Recall.Maui performs the operating-system step and is the only executable. A screen cannot choose a stored width. A platform file cannot invent a content id.

## A folder becomes a grant

```mermaid
flowchart LR
  Person[Person picks a folder]
  Maui[Recall.Maui asks the operating system]
  Core[Recall.Core accepts or refuses]
  UI[Recall.UI shows the result]
  Person --> Maui --> Core --> UI
```

The person picks a folder. Core refuses a blank path, a duplicate, and a folder already inside a grant. A folder that contains smaller grants replaces those grants. Maui returns only the path the operating system allowed. The screen shows that result.

## A text file becomes a hit

```mermaid
flowchart LR
  MauiRead[Recall.Maui reads the file]
  CoreRule[Recall.Core splits and names the chunk]
  Adapt[Recall.Adapters embeds and stores items_text]
  Log[Local op-log on the device]
  MauiRead --> CoreRule --> Adapt --> Log
```

Core chooses the prefix, the split, the content id, and the stored width of 512. Maui reads the granted file and records the hash in checkpoints.db so the same text is not embedded again. Adapters run the text model and write items_text. A new id is appended to the local op-log. The text and the vector stay on the device.

## Find and what else runs

```mermaid
flowchart LR
  Home[Person types on Home]
  CoreQ[Recall.Core embeds the question]
  Index[Recall.Adapters searches items_text]
  Open[Recall.Maui opens the file]
  Home --> CoreQ --> Index --> Open
```

```mermaid
flowchart TB
  Grant[Granted folder]
  Grant --> TextRun[Text rows are written]
  Grant --> AudioRun[Audio windows are written when audio is on]
  Grant --> PictureSearch[A picked JPEG can search]
  PictureSearch --> NoRows[Picture and video rows are not written]
```

Home asks Core to search. Core embeds the question once and cuts it to 512. Adapters fuse the vector list and the keyword list inside items_text and drop a path outside the grant. At most 20 files return. Opening a card asks Maui to open the local file. The result is opened, missing, or failed. Picture search runs when the person picks a JPEG and looks in items_image and items_video. Nothing writes those picture or video rows yet. Audio ingest runs on the MAUI host when the grant has audio on, and it writes windows into items_audio. The audio download follows the vision file and does not fetch a second weight. Speech-to-text is forbidden. An ONNX reader is not constructed.
