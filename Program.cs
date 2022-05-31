using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace main;
public struct works
{
    public string tid { get; set; }

    public string depname { get; set; }

    public string faculty { get; set; }

    public string post { get; set; }

    public string depname_short { get; set; }

    public string faculty_short { get; set; }

    public string pluralist { get; set; }
}
public struct Prepod
{
    public string id { get; set; }

    public string username { get; set; }

    public string lastname { get; set; }

    public string firstname { get; set; }

    public string middlename { get; set; }

    public string auditorium { get; set; }

    public string email { get; set; }

    public string phone { get; set; }

    public string degree_name { get; set; }

    public string degree_description { get; set; }

    public string at_name { get; set; }

    public works[] works { get; set; }
}

public static class Programm
{
    static ConcurrentQueue<Prepod> _prepods = new();

    static ConcurrentQueue<string> _wrongId = new();

    static ConcurrentQueue<string> _logs = new();


    static void Log(string text)
    {
        Console.WriteLine(text);
        _logs.Enqueue(text);
    }


    static void Main(string[] args)
    {
        Log("Starting getting theachers\t" + DateTime.Now);

        var ans = GetPrep();

        Log("Theachers id got\t" + DateTime.Now);

        List<Task> tasks = new();
        for (int i = 0; i < 1; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                while (ans.TryDequeue(out var prepod))
                {                   
                    try
                    {
                        GetInfo(prepod.id);
                    }
                    catch (TaskCanceledException ex)
                    {
                        Console.WriteLine(ex);
                        ans.Enqueue(prepod);
                    }
                    catch (Exception e)
                    {
                        _wrongId.Enqueue(prepod.id);
                        Log("Failed id: " + prepod.id + " \n!!!!!id was not parsed!!!!!\n\twith: " + e.Message + "\t" + DateTime.Now);
                        continue;
                    }

                    Log("Task: " + Task.CurrentId + " finished\twith id: " + prepod.id + "\t" + DateTime.Now);
                    Log("Left: " + ans.Count);
                }
            }));
        }
        Task.WaitAll(tasks.ToArray());

        Log("All theachers got\t" + "total: " + _prepods.Count + "\t" + DateTime.Now);

        StreamWriter writer = new StreamWriter("teachers.json", true);
        writer.WriteLine("\n==========================\n" + DateTime.Now.ToString() + "\n==========================\n");
        foreach (var item in _prepods)
        {
            writer.WriteLine(JsonSerializer.Serialize(item));
        }

        writer.Close();

        StreamWriter writer2 = new("wrong.json", true);
        writer2.WriteLine("\n==========================\n" + DateTime.Now.ToString() + "\n==========================\n");
        foreach (var item in _wrongId)
        {
            writer2.WriteLine(item);
        }

        writer2.Close();
        Log("All info saved\t" + DateTime.Now);


        StreamWriter logWriter = new("log.txt", true);
        logWriter.WriteLine("\n==========================\n" + DateTime.Now.ToString() + "\n==========================\n");
        foreach (var item in _logs)
        {
            logWriter.WriteLine(item);
        }
        logWriter.Close();
    }

    public static void GetInfo(string id)
    {
        try
        {
        TryAgain:
            HttpClient client = new();
            var message = new HttpRequestMessage(HttpMethod.Get, $"https://pro.guap.ru/getuserprofile/{id}");
            message.Headers.Add("Cookie", "PHPSESSID=sstk9d5dio1roe6tgftag0b5qe");
            var reply = client.Send(message);
            
            switch (reply.StatusCode)
            {
                case System.Net.HttpStatusCode.Forbidden:
                    _wrongId.Enqueue(id);
                    return;
                case System.Net.HttpStatusCode.OK:
                    break;
                default:
                    goto TryAgain;
            }
            //var stream = reply.Content.ReadAsStream();

            var str = reply.Content.ReadAsStringAsync().Result;
            var json = JsonNode.Parse(str)!["user"]!.AsObject();

            var value = JsonSerializer.Deserialize<Prepod>(json);
            //dynamic value = JsonSerializer.Deserialize<dynamic>(json)!;


            var works = json["works"]!.AsArray();

            value.works = new works[works.Count];

            for (int i = 0; i < works.Count; i++)
            {
                var w = JsonSerializer.Deserialize<works>(works[i]);
                value.works[i] = w;
            }
            _prepods.Enqueue(value);
        }
        catch (Exception)
        {
            throw;
        }
    }

    public static ConcurrentQueue<Prepod> GetPrep()
    {
        ConcurrentQueue<Prepod> prepods = new();
        HttpClient client = new();
        var message = new HttpRequestMessage(HttpMethod.Post, "https://pro.guap.ru/getDictionariesAndPeople/");
        var reply = client.Send(message);
        var str = reply.Content.ReadAsStringAsync().Result;
        var node = JsonNode.Parse(str);
        var arr = node!["dictionaries"]!["people"]!.AsArray();
        foreach (var item in arr)
        {
            prepods.Enqueue(new Prepod() { id = item!["id"]!.ToString(), username = item["text"]!.ToString() });
        }
        return prepods;
    }
}
