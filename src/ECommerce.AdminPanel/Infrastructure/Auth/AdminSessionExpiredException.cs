namespace ECommerce.AdminPanel.Infrastructure;

/// <summary>Panel oturumu bulunamadı ya da API token'ı yenilenemedi; yönetici tekrar giriş yapmalı.</summary>
public sealed class AdminSessionExpiredException() : Exception("Yönetici oturumu sona erdi.");
