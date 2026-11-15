using Bookstore.Prompts.Evaluation;
using Bookstore.Prompts.Tasks.LongDocument;

namespace Bookstore.Prompts.Tests.LongDocument;

public class ContractTests
{
    static readonly ContractDocument Contract = new("test.md", """
        # KİTAP TEDARİK SÖZLEŞMESİ

        ## Madde 17. İade Koşulları

        17.6. Kusurlu kitapların iadesinde kargo bedeli TEDARİKÇİ'ye aittir.

        ## Ek-3. Değişiklik Protokolü

        Ek-3.2. Okul siparişleri için üst sınır yüzde otuz (%30) olarak uygulanır.
        """);

    [Fact]
    public void Splits_at_second_level_headings()
    {
        Assert.Equal(["Madde 17. İade Koşulları", "Ek-3. Değişiklik Protokolü"], Contract.Sections.Select(s => s.Heading));
    }

    [Fact]
    public void Turkish_capital_letters_match_lower_case_keywords()
    {
        string output = """{"quotes": [{"clause": "17.6", "text": "Kusurlu kitapların iadesinde kargo bedeli TEDARİKÇİ'ye aittir."}], "answer": "Kargo bedelini TEDARİKÇİ öder."}""";

        Verdict verdict = ContractGrader.Grade(Contract, new(["tedarikçi"], ["17.6"]), output, grounded: true);

        Assert.True(verdict.Passed, verdict.Detail);
    }

    [Fact]
    public void Quote_that_is_not_in_the_contract_fails()
    {
        string output = """{"quotes": [{"clause": "Ek-3.2", "text": "Okul siparişleri için üst sınır yüzde kırk (%40) olarak uygulanır."}], "answer": "%40"}""";

        Verdict verdict = ContractGrader.Grade(Contract, new(["%30|yüzde otuz|yüzde 30"], ["Ek-3"]), output, grounded: true);

        Assert.Equal("answer \"%40\" lacks %30|yüzde otuz|yüzde 30; 1/1 quotes not in the contract", verdict.Detail);
    }

    [Fact]
    public void Question_without_an_answer_expects_not_found()
    {
        Verdict verdict = ContractGrader.Grade(Contract, new([], [], NotFound: true), """{"quotes": [], "answer": "NOT_FOUND"}""", grounded: true);

        Assert.True(verdict.Passed);
    }
}
