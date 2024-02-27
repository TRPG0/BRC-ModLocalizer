# ModLocalizer

A library for reading custom `.fods` language files to help make it easier to add multi-language support to mods.

For an example, see the [MoreMap repository.](https://github.com/TRPG0/BRC-MoreMap)

## Creating your own language files

Bomb Rush Cyberfunk stores its language information in files with the extension `.fods`. These files can be opened in a program such as [LibreOffice](https://www.libreoffice.org/download/download-libreoffice/) Calc, which is free and open-source.

You can use the game's existing language files as a starting point, which are located at `...\BombRushCyberfunk\Bomb Rush Cyberfunk_Data\StreamingAssets\Languages`. At the bottom of the window, you will see that the file has 11 sheets, labeled `Text`, `Sizes`, `Dialogue`, `Examples`, `Credits`, `CharacterNames`, `StageNames`, `ObjectiveText`, `EmailMessages`, `Notifications`, and `SkinText`. Note that even if you don't plan on using them, **all of these sheets must exist in the file or else it will not be readable in game.** In most cases, just using the `Text` sheet and deleting the contents of the other sheets will be fine, but do not delete the sheets themselves.

In most of the sheets, you will see a code, a translation, and a comment. The code is used in your plugin to retrieve the translation, and the comment is for any additional notes or context that you may want to provide when other people are tranlsating for you.

Once you have as many codes as you need, you can save the file. The name must exactly match one of the languages in Unity's [SystemLanguage](https://docs.unity3d.com/ScriptReference/SystemLanguage.html) enum for it to read properly. Officially, Bomb Rush Cyberfunk supports 9 languages (Dutch, English, French, German, Italian, Japanese, Portuguese, Russian, and Spanish), but theoretically, other languages could be supported via mods in the same way if there is a `.fods` file for it in the game's Languages folder.


## Using ModLocalizer in your plugin

First, you will need an instance of the `PluginLocalizer` class. The constructor requires the name of your plugin, and the path to your language files. Optionally, you can change the default language to something other than English if you need to.

```csharp
public PluginLocalizer Localizer { get; private set; }

private void Awake()
{
    string assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
    Localizer = new PluginLocalizer("MoreMap", Path.Combine(assemblyPath, "Languages"));
}
```

### Reading from the language files

Before you can use your `PluginLocalizer`, it will need to be initialized, which happens as soon as the main menu is first loaded. Attempting to read anything from the language files before then will just return the key instead of the localized string.

After initialization, you can read from your language files with `GetRawTextValue`, where the subgroup is the sheet to read from, and the localization key is the code.

```csharp
title.text = Localizer.GetRawTextValue(Subgroups.Text, "APP_MAP_HEADER");
```

### Font types

In Bomb Rush Cyberfunk, nearly every `TextMeshProUGUI` component is paired with a `TMProFontLocalizer` component, which automatically changes the font depending on which language is in use. Each `TMProFontLocalizer` has a field for a `GameFontType`, which you can easily load with `LoadFontType`.

You can also use `LoadFontTypeAndRemoveUnusedFonts` to remove any fonts that are associated with a language that does not have a matching file in your plugin, or `LoadFontTypeEnglishOnly` to remove all fonts except the latin font used for English. These are useful because the strings retrieved from the file will be in the default language, but they will still change fonts if left as is.

The `GameFontTypes` enum contains every font type used in the game, for easy access.

```csharp
public GameFontType PhoneFont = Localizer.LoadFontTypeAndRemoveUnusedFonts(GameFontTypes.PhoneMainText);
```


### Events

`PluginLocalizer` has two events that you can subscribe to, `OnLanguageChanged` and `OnInitializationFinished`.

As the name implies, `OnLanguageChanged` is raised whenever the language is changed in the options menu. If there is no file for the selected language, it will fallback to the default language instead.

```csharp
private void Awake()
{
    Localizer.OnLanguageChanged += LanguageChanged;
}

private void OnDestroy()
{
    Localizer.OnLanguageChanged -= LanguageChanged;
}
```

`OnInitializationFinished` is raised when the main menu is loaded for the first time. This event is useful for loading font types.

```csharp
Localizer.OnInitializationFinished += () =>
{
    PhoneFont = Localizer.LoadFontTypeAndRemoveUnusedFonts(GameFontTypes.PhoneMainText);
};
```