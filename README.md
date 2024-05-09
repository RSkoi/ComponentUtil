![componentutil_preview_](https://github.com/RSkoi/ComponentUtil/assets/31830382/28fc7d7c-5ff9-4a2f-9a06-708e5f17ef53)

# ComponentUtil

Koikatsu studio BepInEx plugin that adds a simple UI editor for Unity `Component` properties and fields. Allows for limited inspection and editing of primitive types. ComponentUtil tracks changed properties / fields and their default values, and saves changes into the Koikatsu scene file.

Use of ComponentUtil is limited to items in the workspace of the scene. It does *not* cover all property / field value types and should *not* be used to overwrite values explicitly managed by other plugins. For in-depth debugging of *all* objects within the Unity scene, use the [Runtime Unity Editor](https://github.com/ManlyMarco/RuntimeUnityEditor).

Messing around with properties or fields of important `Component`s can corrupt or otherwise mess up a Koikatsu scene, depending on what you do to it. Generally, if another plugin exists with the purpose of editing a specific type of item, you should use that plugin and *not* use ComponentUtil for said item to avoid conflicts. See **Apply data on scene load** setting below if you encounter corrupted scenes.

## How-To-Use / Basics

Each item in the studio workspace represents a so-called transform hierarchy of Unity `GameObject`s. Each hierarchy holds a variable amount of `GameObject`s. Each `GameObject` holds a variable amount of `Component`s. Each `Component` holds a variable amount of properties and fields. ComponentUtil finds all `GameObject`s of the selected item, their `Component`s, their properties and fields, and lists said things in paged, searchable lists.

1. Select `Transform` entry in the TransformList by clicking on it.
2. Select `Component` entry in the ComponentList by clicking on it.
3. View, edit and reset values in the ComponentInspector. Edited entries are marked green.

## BepInEx Config

- **UI scale:** Scales the UI to given factor. Reopen ComponentUtil window for the change to apply. (Default: 0.7)
- **Toggle UI:** Keyboard combo to toggle the UI of ComponentUtil. (Default: M+RightControl)
- **Items per page:** How many items to display in the transform / component list per page. Don't set this too high. (Default: 9)
- **Wait time after scene load:** How long ComponentUtil should wait in seconds after a scene is loaded before applying tracked changes. Try setting this higher if after loading a scene the changes saved with ComponentUtil seem to be overwritten. (Default: 2)
- **Apply data on scene load:** Whether ComponentUtil should apply related saved data on scene load. Set to false for debugging purposes or if a scene fails to load due to ComponentUtil. (Default: true)
- **Save data on scene save:** Whether ComponentUtil should save related data on scene save. Set to false for debugging purposes or if you want to save a 'clean' scene with no edits. (Default: true)

## Supported Types

### Explicitly supported types

- Enums
- `float`
- `double`
- `decimal`
- `bool`
- `int`
- `uint`
- `short`
- `ushort`
- `long`
- `ulong`
- `byte`
- `sbyte`
- `nint`
- `nuint`

### Explicitly NOT supported types

- reference types (this includes `string`)
- properties without a public `get` method

### Types to be supported in the future (probably) (maybe)

- `Vector2`, `Vector3`, `Vector4`
- `Quaternion`
- `UnityEngine.Color`

## Known Quirks

- Not all properties expose a public `set` method. These entries are marked as read-only / non-interactable. Properties without a public `get` method will not be listed at all.
- Performance is affected by the **Items per page** config setting. The more UI items per page, the more hiccups and stutters you may notice when toggling or interacting with the UI. All must bow to the garbage collector.
- When loading a Koikatsu scene with saved property / field edits, the changes ComponentUtil applies after the scene finished loading in are sometimes overwritten. Presumably because God said so. God's will can be circumvented by waiting a certain amount of seconds after loading has finished. If in need, the amount of seconds to wait can be changed with the **Wait time after scene load** setting.
