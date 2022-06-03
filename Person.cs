using NewParser.Works;
namespace NewParser.Person;

public struct Person
{
    public string id { get; set; }

    public string username { get; set; }

    public string lastname { get; set; }

    public string firstname { get; set; }

    public string middlename { get; set; }

    public string auditorium { get; set; }

    public string email { get; set; }

    public string phone { get; set; }

    public string degree_id { get; set; }

    public string degree_name { get; set; }

    public string degree_description { get; set; }

    public string at_name { get; set; }

    public string at_id { get; set; }

    public works[] works { get; set; }
}
