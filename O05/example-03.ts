const defaultPreferences = {
  language: "tr",
  pageSize: 20,
  notifications: { email: true, sms: false },
};

type Preferences = typeof defaultPreferences;

function applyPreferences(preferences: Preferences): void {
  console.log(preferences.language, preferences.notifications.sms);
}

applyPreferences({ language: "en", pageSize: 50, notifications: { email: false, sms: true } });
