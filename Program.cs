using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Net;
using NLog;
using NewParser.Person;
using NewParser.Works;


namespace Main;

public static class Programm
{
    static ConcurrentQueue<Person> _prepods = new();

    static ConcurrentQueue<string> _wrongId = new();

    static HttpClient _client = new();

    static string _cookie = "gu3anmbje0b27m93oud26u3iqt";

    private static System.Timers.Timer _aTimer = null;

    private static Logger _logger = LogManager.GetLogger("logfileRules");

    private static ulong _time = 2 * 1000;


    static void Log(string text)
    {
        _logger.Info(text);
    }

    private static void SetTimer()
    {
        StreamReader reader = new("info.txt");
        reader.Close();
        _aTimer = new System.Timers.Timer(_time);
        _aTimer.Elapsed += ATimer_Elapsed;
        _aTimer.AutoReset = true;
        _aTimer.Enabled = true;
    }

    private static void TimerSetNow()
    {
        StreamWriter writer = new("info.txt", false);
        writer.WriteLine(JsonSerializer.Serialize(DateTime.Now));
        writer.Close();
    }

    private static DateTime TimerGetTime()
    {

            try
            {
                StreamReader reader = new("info.txt");
                var json = reader.ReadLine();
                var time = JsonSerializer.Deserialize<DateTime>(json);
                reader.Close();
                return time;
            }
            catch (FileNotFoundException)
            {
                return DateTime.MinValue;
            }

    }

    private static void ATimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        _aTimer.Stop();
        var time = TimerGetTime();
        if ((ulong)(DateTime.Now - time).TotalMilliseconds >= _time)
        {
            if (Manager())
            {
                Log($"Everything is ok, next check in {_time / 3600} hours");
                TimerGetTime();
                Filler.Filler.Fill();
            }
            else
            {
                Log("Something went wrong. Trying Againg in 6 min");
                Thread.Sleep(360000);
                if (Manager())
                {
                    Log($"Everything is ok, next check in {_time / 3600} hours");
                    TimerGetTime();
                    Filler.Filler.Fill();
                }
                else
                {
                    Log($"Something went wrong. Trying Againg in {_time / 3600} hours");
                    TimerSetNow();
                }
            }
        }
        _aTimer.Start();
    }

    static bool Manager()
    {
        try
        {
            Log("Starting getting theachers\t");

            var ans = GetPrep();

            Log("Theachers id got\t");

            List<Task> tasks = new();
            for (int i = 0; i < 8; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    while (ans.TryDequeue(out var id))
                    {
                        try
                        {
                            GetInfo(id);
                        }
                        catch (TaskCanceledException ex)
                        {
                            Console.WriteLine(ex + "\t" + id);
                            ans.Enqueue(id);
                        }
                        catch (Exception e)
                        {
                            if (e.Message == "Forbidden")
                            {
                                Log("Task: " + Task.CurrentId + "\twith id: " + id + " forbidden\t");
                            }
                            else
                            {
                                Log("Failed id: " + id + " \n!!!!!id was not parsed!!!!!\nwith: " + e.Message + "\t");
                            }
                            _wrongId.Enqueue(e.Message + " id: " + id);
                            continue;
                        }

                        Log("Task: " + Task.CurrentId + " finished\twith id: " + id + "\t");
                        Log("Left: " + ans.Count);
                    }
                }));
            }
            Task.WaitAll(tasks.ToArray());

            Log("All theachers got\t" + "total: " + _prepods.Count + "\t");

            try
            {
                SaveData();
            }
            catch (Exception e)
            {
                throw new Exception("Saving data exception\n" + e.Message);
            }
            return true;
        }
        catch (Exception)
        {
            throw;
        }
    }

    private static void SaveData()
    {
        try
        {
            StreamWriter writer = new($"teachers.json", false);
            writer.WriteLine(JsonSerializer.Serialize(_prepods.ToArray()));
            writer.Close();
            Log("teachers info saved\t");

            writer = new("wrong.json", true);
            writer.WriteLine("\n==========================\n" + DateTime.Now.ToString() + "\n==========================\n");
            writer.WriteLine(JsonSerializer.Serialize(_wrongId));
            writer.Close();
            Log("wrong id info saved\t");

            TimerSetNow();
        }
        catch (Exception)
        {
            Log("Info saving error\t");
            throw;
        }
       
    }

    public static void GetInfo(string id)
    {
        try
        {
        TryAgain:
            
            var message = new HttpRequestMessage(HttpMethod.Get, $"https://pro.guap.ru/getuserprofile/{id}");
            message.Headers.Add("Cookie", $"PHPSESSID={_cookie}");
            var reply = _client.Send(message);
            
            switch (reply.StatusCode)
            {
                case HttpStatusCode.Forbidden:
                    throw new Exception("Forbidden");
                case HttpStatusCode.OK:
                    break;
                default:
                    goto TryAgain;
            }

            var str = reply.Content.ReadAsStringAsync().Result;
            var json = JsonNode.Parse(str)!["user"]!.AsObject();

            var value = JsonSerializer.Deserialize<Person>(json);


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

    public static ConcurrentQueue<string> GetPrep()
    {
        ConcurrentQueue<string> prepods = new();
        HttpClient client = new();
        var message = new HttpRequestMessage(HttpMethod.Post, "https://pro.guap.ru/getDictionariesAndPeople/");
        var reply = client.Send(message);
        var str = reply.Content.ReadAsStringAsync().Result;
        var node = JsonNode.Parse(str);
        var arr = node!["dictionaries"]!["people"]!.AsArray();
        foreach (var item in arr)
        {
            prepods.Enqueue(item!["id"]!.ToString());
        }
        return prepods;
    }

    static void Main(string[] args)
    {
        _time = Convert.ToUInt64(Environment.GetEnvironmentVariable("TIME_SPAN") ?? throw new ArgumentException("NO TIME_SPAN")) * 1000;

        _cookie = Environment.GetEnvironmentVariable("COOKIE") ?? throw new ArgumentException("NO COOKIE");

        ServicePointManager.DefaultConnectionLimit = 10;


        SetTimer();

        while (true) { }
    }
}
