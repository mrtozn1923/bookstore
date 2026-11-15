// Local stand-in for a third-party analytics tag: no network calls, no real tag ID.
window.__analyticsStubLoaded = true;
document.cookie = '_stub_aid=' + Math.random().toString(36).slice(2) + '; path=/; max-age=31536000; SameSite=Lax';
