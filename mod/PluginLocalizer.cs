using Reptile;
using System.Collections.Generic;
using System;
using System.IO;
using UnityEngine;
using HarmonyLib;

namespace ModLocalizer
{
    public class PluginLocalizer
    {
        private string pluginName;
        private SystemLanguage defaultLanguage;
        private string languageFilePath;
        public bool Initialized { get; private set; } = false;

        private SystemLanguage localizationLanguage;
        private PlatformLanguages platformLanguages;
        private LocalizationLookupTable localizationLookupTable;
        private LocalizationData localizationData;

        public event OnLanguageChangedDelegate OnLanguageChanged;
        public delegate void OnLanguageChangedDelegate(SystemLanguage language);

        public event OnInitializationFinishedDelegate OnInitializationFinished;
        public delegate void OnInitializationFinishedDelegate();

        public PluginLocalizer(string pluginName, string languageFilePath, SystemLanguage defaultLanguage = SystemLanguage.English)
        {
            this.pluginName = pluginName;
            this.defaultLanguage = defaultLanguage;
            this.languageFilePath = languageFilePath;
            platformLanguages = CheckAvailableLanguages();
            Core.Instance.Localizers.Add(this);
        }

        private PlatformLanguages CheckAvailableLanguages()
        {
            PlatformLanguages platformLanguages = ScriptableObject.CreateInstance<PlatformLanguages>();
            string[] files = Directory.GetFiles(languageFilePath, "*");
            List<SystemLanguage> languages = new List<SystemLanguage>();
            foreach (string file in files)
            {
                if (Path.GetExtension(file) == ".fods")
                {
                    string fileName = Path.GetFileNameWithoutExtension(file);
                    SystemLanguage result;
                    if (Enum.TryParse(fileName, out result))
                    {
                        Core.Logger.LogInfo($"Found language {result} for {pluginName}");
                        languages.Add(result);
                    }
                }
            }
            platformLanguages.availableLanguages = languages.ToArray();
            return platformLanguages;
        }

        internal void Initialize()
        {
            if (Initialized) return;
            Initialized = true;

            localizationData = Reptile.Core.Instance.localizerData;
            SystemLanguage language = Reptile.Core.Instance.Localizer.Language;

            if (OnInitializationFinished != null) OnInitializationFinished();

            if (IsLanguageAvailable(language)) UpdateLocalization(language);
            else UpdateLocalization(defaultLanguage);

            Core.Logger.LogInfo($"Finished initialization for {pluginName}");
        }

        private bool IsLanguageAvailable(SystemLanguage language)
        {
            if (!Initialized)
            {
                Core.Logger.LogWarning($"Localizer with name {pluginName} has not been initialized yet!");
                return false;
            }
            for (int i = 0; i < platformLanguages.availableLanguages.Length; i++)
            {
                if (platformLanguages.availableLanguages[i] == language) return true;
            }
            return false;
        }

        private LocalizationLookupTable GenerateLocalizationTable(SystemLanguage language)
        {
            string path = Path.Combine(languageFilePath, language.ToString() + ".fods");
            LocalizationTableGenerator localizationTableGenerator = new LocalizationTableGenerator();
            return localizationTableGenerator.GetLocalizationLookupTable(path, localizationData, language);
        }

        private LocalizationLookupTable HandleNoLocalizationFoundForLanguage(SystemLanguage language)
        {
            return CreateDefaultLocalizationTable();
        }

        private LocalizationLookupTable CreateDefaultLocalizationTable()
        {
            return GenerateLocalizationTable(defaultLanguage);
        }

        private void GenerateLocalization()
        {
            localizationLookupTable = GenerateLocalizationTable(localizationLanguage);
            if (localizationLookupTable == null) localizationLookupTable = HandleNoLocalizationFoundForLanguage(localizationLanguage);
        }

        private void DisposeCurrentLocalizationLookupTable()
        {
            if (localizationLookupTable != null) localizationLookupTable.Dispose();
            localizationLookupTable = null;
        }

        internal void UpdateLocalization(SystemLanguage language)
        {
            if (localizationLanguage == language) return;

            DisposeCurrentLocalizationLookupTable();
            localizationLanguage = language;
            GenerateLocalization();
            if (OnLanguageChanged != null) OnLanguageChanged(language);
        }

        public string GetRawTextValue(Subgroups subgroup, string localizationKey)
        {
            if (!Initialized)
            {
                Core.Logger.LogWarning($"Localizer with name {pluginName} has not been initialized yet!");
                return localizationKey;
            }
            return localizationLookupTable.GetLocalizationValueFromSubgroup(subgroup.ToString(), localizationKey);
        }

        public GameFontType LoadFontType(GameFontTypes fontType)
        {
            return Reptile.Core.Instance.Assets.LoadAssetFromBundle<GameFontType>("coreassets", $"assets/gamemanagement/localization/scriptableobjects/fonts/gamefonttypes/{fontType}.asset");
        }

        public GameFontType LoadFontTypeAndRemoveUnusedFonts(GameFontTypes fontType)
        {
            GameFontType gameFontType = LoadFontType(fontType);
            if (!Initialized)
            {
                Core.Logger.LogWarning($"Localizer with name {pluginName} has not been initialized yet!");
                return gameFontType;
            }

            Traverse traverse = Traverse.Create(gameFontType);
            TextMeshProFont[] oldArray = traverse.Field<TextMeshProFont[]>("fonts").Value;
            List<TextMeshProFont> newList = new List<TextMeshProFont>();

            for (int i = 0; i < oldArray.Length; i++)
            {
                if (IsLanguageAvailable(oldArray[i].Language)) newList.Add(oldArray[i]);
            }

            if (newList.Count < 1) throw new Exception("No valid fonts found for this GameFontType. This should never happen.");

            traverse.Field<TextMeshProFont[]>("fonts").Value = newList.ToArray();
            return gameFontType;
        }

        public GameFontType LoadFontTypeEnglishOnly(GameFontTypes fontType)
        {
            GameFontType gameFontType = LoadFontType(fontType);
            if (!Initialized)
            {
                Core.Logger.LogWarning($"Localizer with name {pluginName} has not been initialized yet!");
                return gameFontType;
            }

            Traverse traverse = Traverse.Create(gameFontType);
            TextMeshProFont[] oldArray = traverse.Field<TextMeshProFont[]>("fonts").Value;
            List<TextMeshProFont> newList = new List<TextMeshProFont>();

            for (int i = 0; i < oldArray.Length; i++)
            {
                if (oldArray[i].Language == SystemLanguage.English) newList.Add(oldArray[i]);
            }

            if (newList.Count < 1) throw new Exception("No valid fonts found for this GameFontType. This should never happen.");

            traverse.Field<TextMeshProFont[]>("fonts").Value = newList.ToArray();
            return gameFontType;
        }
    }

    public enum Subgroups
    {
        Text,
        Sizes,
        Dialogue,
        Examples,
        Credits,
        CharacterNames,
        StageNames,
        ObjectiveText,
        EmailMessages,
        Notifications,
        SkinText
    }

    public enum GameFontTypes
    {
        CharacterNameText,
        CreditsNameText,
        CreditsTitleText,
        DefaultText,
        DefaultTextBold,
        DefaultTextHighlight,
        DefaultTextThin,
        DottedText,
        FlavorText,
        GameplayHudCrewText,
        GameplayHudGraffitiText,
        GameplayHudHeaderText,
        GameplayHudLocalizedGraffitiText,
        GameplayHudStatusText,
        LoadingText,
        MainButtonText,
        MainButtonTextBackdrop,
        MainButtonTextHighlight,
        MainButtonTextPressed,
        MenuTimelineSelectedText,
        MenuTimelineText,
        MenuTitlesText,
        MicroBoyText,
        MicroBoyTextWithoutOutline,
        MicroBoyTitle,
        MicroBoyTitleWithoutOutline,
        PhoneClockText,
        PhoneFortuneText,
        PhoneGraffitiSizeText,
        PhoneLocalizedNotifications,
        PhoneMainText,
        PhoneMessageText,
        PhoneMsgSubjectText,
        PhoneNotificationText,
        PhoneOnScreenNotificationText,
        PhoneSubtext,
        YesNoButtonBackdropText,
        YesNoButtonText
    }
}
