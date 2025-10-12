using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Modus für die Gravatar-Karte
/// </summary>
public enum GravatarCardMode
{
    /// <summary>
    /// Verwendet ein Iframe zur Anzeige der offiziellen Gravatar-Profilkarte
    /// </summary>
    Iframe,

    /// <summary>
    /// Lädt die Profildaten über die API und erstellt eine benutzerdefinierte Karte
    /// </summary>
    Custom
}

/// <summary>
/// Gravatar Profildaten Modell
/// </summary>
public class GravatarProfile
{
    [JsonPropertyName("hash")]
    public string Hash { get; set; }

    [JsonPropertyName("display_name")]
    public string DisplayName { get; set; }

    [JsonPropertyName("profile_url")]
    public string ProfileUrl { get; set; }

    [JsonPropertyName("avatar_url")]
    public string AvatarUrl { get; set; }

    [JsonPropertyName("avatar_alt_text")]
    public string AvatarAltText { get; set; }

    [JsonPropertyName("location")]
    public string Location { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonPropertyName("job_title")]
    public string JobTitle { get; set; }

    [JsonPropertyName("company")]
    public string Company { get; set; }

    [JsonPropertyName("verified_accounts")]
    public List<VerifiedAccount> VerifiedAccounts { get; set; }

    [JsonPropertyName("pronunciation")]
    public string Pronunciation { get; set; }

    [JsonPropertyName("pronouns")]
    public string Pronouns { get; set; }

    [JsonPropertyName("timezone")]
    public string Timezone { get; set; }

    [JsonPropertyName("languages")]
    public List<Language> Languages { get; set; }

    [JsonPropertyName("first_name")]
    public string FirstName { get; set; }

    [JsonPropertyName("last_name")]
    public string LastName { get; set; }

    [JsonPropertyName("is_organization")]
    public bool IsOrganization { get; set; }

    [JsonPropertyName("header_image")]
    public string HeaderImage { get; set; }

    [JsonPropertyName("hide_default_header_image")]
    public bool HideDefaultHeaderImage { get; set; }

    [JsonPropertyName("background_color")]
    public string BackgroundColor { get; set; }

    [JsonPropertyName("links")]
    public List<Link> Links { get; set; }

    [JsonPropertyName("interests")]
    public List<Interest> Interests { get; set; }

    [JsonPropertyName("payments")]
    public Payments Payments { get; set; }

    [JsonPropertyName("contact_info")]
    public ContactInfo ContactInfo { get; set; }

    [JsonPropertyName("gallery")]
    public List<GalleryImage> Gallery { get; set; }

    [JsonPropertyName("number_verified_accounts")]
    public int NumberVerifiedAccounts { get; set; }

    [JsonPropertyName("last_profile_edit")]
    public string LastProfileEdit { get; set; }

    [JsonPropertyName("registration_date")]
    public string RegistrationDate { get; set; }
}

/// <summary>
/// Verifiziertes Social-Media-Konto
/// </summary>
public class VerifiedAccount
{
    [JsonPropertyName("service_type")]
    public string ServiceType { get; set; }

    [JsonPropertyName("service_label")]
    public string ServiceLabel { get; set; }

    [JsonPropertyName("service_icon")]
    public string ServiceIcon { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; }

    [JsonPropertyName("is_hidden")]
    public bool IsHidden { get; set; }
}

/// <summary>
/// Link auf dem Profil
/// </summary>
public class Link
{
    [JsonPropertyName("label")]
    public string Label { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; }
}

/// <summary>
/// Interesse/Hobby
/// </summary>
public class Interest
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("slug")]
    public string Slug { get; set; }
}

/// <summary>
/// Sprache
/// </summary>
public class Language
{
    [JsonPropertyName("code")]
    public string Code { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("is_primary")]
    public bool IsPrimary { get; set; }

    [JsonPropertyName("order")]
    public int Order { get; set; }
}

/// <summary>
/// Galeriebild
/// </summary>
public class GalleryImage
{
    [JsonPropertyName("url")]
    public string Url { get; set; }

    [JsonPropertyName("alt_text")]
    public string AltText { get; set; }
}

/// <summary>
/// Zahlungsinformationen
/// </summary>
public class Payments
{
    [JsonPropertyName("crypto_wallets")]
    public List<CryptoWallet> CryptoWallets { get; set; }
}

/// <summary>
/// Krypto-Wallet
/// </summary>
public class CryptoWallet
{
    [JsonPropertyName("label")]
    public string Label { get; set; }

    [JsonPropertyName("address")]
    public string Address { get; set; }
}

/// <summary>
/// Kontaktinformationen
/// </summary>
public class ContactInfo
{
    [JsonPropertyName("email")]
    public string Email { get; set; }

    [JsonPropertyName("contact_form")]
    public string ContactForm { get; set; }

    [JsonPropertyName("calendar")]
    public string Calendar { get; set; }
}