var REG_NONE = NewRegistrar("none");
var DNS_HOST = NewDnsProvider("bind");

D("bookstore.example", REG_NONE, DnsProvider(DNS_HOST),
    DefaultTTL(3600),
    NAMESERVER("ns1.dnshost.example."),
    SOA("@", "ns1.dnshost.example.", "hostmaster.bookstore.example.", 7200, 3600, 1209600, 300),
    A("@", "203.0.113.10"),
    AAAA("@", "2001:db8:10::10"),
    CNAME("www", "bookstore.example."),
    A("staging", "203.0.113.11"),
    // App Service: CNAME + asuid verification TXT
    CNAME("api", "bookstore-api.azurewebsites.net."),
    TXT("asuid.api", "9ABEB8D35A69D8E0F4A0BFF18E629B9F43A21C43C122B267F86E53CCD94D5DDD"),
    CAA("@", "issue", "letsencrypt.org"),
    CAA("@", "issue", "digicert.com"),
    CAA("@", "issuewild", ";"),
    // owner: marketing, Search Console
    TXT("@", "google-site-verification=pX3c9QeLr0vT2mWb8yKdN5hJ7sFa1uZo4iGyE6"),
    MX("@", 10, "mx1.corpmail.example."),
    MX("@", 20, "mx2.corpmail.example."),
    TXT("@", "v=spf1 include:_spf.corpmail.example -all"),
    TXT("_dmarc", "v=DMARC1; p=none; rua=mailto:dmarc-reports@bookstore.example")
);
