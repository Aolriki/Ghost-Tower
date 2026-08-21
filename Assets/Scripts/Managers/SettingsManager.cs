using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

// Global singleton (DontDestroyOnLoad) that controls player settings, currently only language.
public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    [Header("Language")]
    [SerializeField] private TMP_Dropdown languageDropdown;

    private const string LocaleCodeKey = "settings_locale_code";

    private List<Locale> _availableLocales = new List<Locale>();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnEnable()
    {
        StartCoroutine(InitializeLocalization());
    }

    void OnDisable()
    {
        if (languageDropdown != null)
            languageDropdown.onValueChanged.RemoveListener(OnLanguageDropdownChanged);
    }

    // Waits for the Localization package to finish initializing before reading the available locales.
    private IEnumerator InitializeLocalization()
    {
        yield return LocalizationSettings.InitializationOperation;

        _availableLocales = new List<Locale>(LocalizationSettings.AvailableLocales.Locales);

        PopulateLanguageDropdown();
        ApplySavedLocale();

        if (languageDropdown != null)
            languageDropdown.onValueChanged.AddListener(OnLanguageDropdownChanged);
    }

    // Builds the dropdown options directly from the locales configured in Localization Settings.
    private void PopulateLanguageDropdown()
    {
        if (languageDropdown == null) return;

        List<string> options = new List<string>();
        foreach (Locale locale in _availableLocales)
            options.Add(locale.LocaleName);

        languageDropdown.ClearOptions();
        languageDropdown.AddOptions(options);
    }

    // Applies the locale saved in PlayerPrefs, or keeps the Localization Settings default if there is no save yet.
    private void ApplySavedLocale()
    {
        string savedCode = PlayerPrefs.GetString(LocaleCodeKey, string.Empty);
        Locale targetLocale = null;

        if (!string.IsNullOrEmpty(savedCode))
            targetLocale = _availableLocales.Find(locale => locale.Identifier.Code == savedCode);

        if (targetLocale == null)
            targetLocale = LocalizationSettings.SelectedLocale;

        SetLocale(targetLocale, save: false);
    }

    private void OnLanguageDropdownChanged(int index)
    {
        if (index < 0 || index >= _availableLocales.Count) return;
        SetLocale(_availableLocales[index], save: true);
    }

    private void SetLocale(Locale locale, bool save)
    {
        if (locale == null) return;

        LocalizationSettings.SelectedLocale = locale;

        if (languageDropdown != null)
        {
            int index = _availableLocales.IndexOf(locale);
            if (index >= 0)
                languageDropdown.SetValueWithoutNotify(index);
        }

        if (!save) return;

        PlayerPrefs.SetString(LocaleCodeKey, locale.Identifier.Code);
        PlayerPrefs.Save();
    }
}