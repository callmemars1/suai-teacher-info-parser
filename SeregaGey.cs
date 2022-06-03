using Npgsql;
using System.Text.Json;
using NewParser.Person;

namespace Filler;

public static class Filler
{
    public static void Fill()
    {
        var dbHost = Environment.GetEnvironmentVariable("DB_HOST") ?? throw new ArgumentException("NO DB_HOST ENV");
        var dbName = Environment.GetEnvironmentVariable("DB_NAME") ?? throw new ArgumentException("NO DB_NAME ENV");
        var dbUser = Environment.GetEnvironmentVariable("DB_USER") ?? throw new ArgumentException("NO DB_USER ENV");
        var dbPass = Environment.GetEnvironmentVariable("DB_PASS") ?? throw new ArgumentException("NO DB_PASS ENV");

        string myDB = "User Id=postgres;Password=1;host=localhost;database=TeacherInfo;";
        string hisDB = $"User Id={dbUser};Password={dbPass};host={dbHost};database={dbName};";

        StreamReader reader = new(@"E:\Kars\NewParser\NewParser\bin\Debug\net6.0\testPrepods.json");
        var json = reader.ReadToEnd();
        reader.Close();

        NpgsqlConnection conn = new(hisDB);
        NpgsqlCommand cmd = new();
        conn.Open();
        cmd.Connection = conn;


        #region deleting

        cmd.CommandText = "delete from teachers_academic_degrees";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "delete from teachers_departments";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "delete from teachers_positions";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "delete from academic_degrees";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "delete from positions";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "delete from departments";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "delete from institutes";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "delete from teachers";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "delete from teaching_degrees";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "delete from class_rooms";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "delete from phones";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "delete from emails";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "delete from persons";
        cmd.ExecuteNonQuery();


        #endregion

        #region academic_degrees
        var degree_id_discription_list = (from p in JsonSerializer.Deserialize<Person[]>(json)
                                          where p.degree_id != "0"
                                          select p.degree_id + "\n" + p.degree_description).ToList();

        Dictionary<int, string> id = new();
        foreach (var item in degree_id_discription_list)
        {
            var ans = item.Split('\n');
            id.TryAdd(Convert.ToInt32(ans[0]), ans[1]);
        }

        foreach (var item in id)
        {
            try
            {
                cmd.CommandText = $"INSERT INTO academic_degrees(id, name) VALUES ({item.Key},'{item.Value}')";
                cmd.ExecuteNonQuery();
            }
            catch (PostgresException e)
            {
                if (e.Message.Contains("23505"))
                {
                    continue;
                }
                throw;
            }
            catch (Exception)
            {
                throw;
            }
        }
        #endregion

        #region teaching_degrees
        var at_id_discription_list = (from p in JsonSerializer.Deserialize<Person[]>(json)
                                      where p.at_id != "0"
                                      select p.at_id + "\n" + p.at_name);

        id = new();
        foreach (var item in at_id_discription_list)
        {
            var ans = item.Split('\n');
            id.TryAdd(Convert.ToInt32(ans[0]), ans[1]);
        }

        foreach (var item in id)
        {
            try
            {
                cmd.CommandText = $"INSERT INTO teaching_degrees(id, name) VALUES ({item.Key}, '{item.Value}')";
                cmd.Connection = conn;

                cmd.ExecuteNonQuery();
            }
            catch (PostgresException e)
            {
                if (e.Message.Contains("23505"))
                {
                    continue;
                }
                throw;
            }
            catch (Exception)
            {
                throw;
            }
        }
        #endregion

        #region institutes

        var values = JsonSerializer.Deserialize<Person[]>(json);
        Dictionary<string, string> institutes = new();

        foreach (var item in values!)
        {
            foreach (var work in item.works)
            {
                institutes.TryAdd(work.faculty, work.faculty_short);
            }
        }


        foreach (var item in institutes)
        {
            try
            {
                cmd.CommandText = $"INSERT INTO institutes(name, number) VALUES ('{item.Key}', '{item.Value}')";
                cmd.ExecuteNonQuery();
            }
            catch (PostgresException e)
            {
                if (e.Message.Contains("23505"))
                {
                    continue;
                }
                throw;
            }
            catch (Exception)
            {
                throw;
            }
        }

        #endregion

        #region departments

        Dictionary<string, string> departments = new();


        foreach (var item in values!)
        {
            foreach (var work in item.works)
            {
                if (departments.TryAdd(work.depname, work.depname_short))
                {
                    try
                    {
                        cmd.CommandText = $"INSERT INTO departments(name, number, institute_id) VALUES ('{work.depname}', '{work.depname_short}', (select id from institutes where name = '{work.faculty}'))";
                        cmd.ExecuteNonQuery();
                    }
                    catch (PostgresException e)
                    {
                        if (e.Message.Contains(23505.ToString()))
                        {
                            continue;
                        }
                        throw;
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                }
            }
        }

        #endregion

        #region positions

        var teachers_list = (from p in JsonSerializer.Deserialize<Person[]>(json)
                             select p);

        List<(string, string)> positions = new();

        foreach (var teacher in teachers_list)
        {
            foreach (var work in teacher.works)
            {
                if (positions.Contains((work.post, work.depname)))
                {
                    continue;
                }
                positions.Add((work.post, work.depname));
                cmd.CommandText = $"INSERT INTO positions(name, department_id) VALUES ('{work.post}', (select id from departments where name = '{work.depname}'))";
                cmd.ExecuteNonQuery();

            }
        }
        #endregion

        #region class_rooms persons emails phones teachers

        var unparsed = JsonSerializer.Deserialize<Person[]>(json);


        foreach (var item in unparsed)
        {
            if (item.auditorium != string.Empty)
            {
                cmd.CommandText = $"INSERT INTO class_rooms(number) VALUES ('{item.auditorium}')";
                cmd.ExecuteNonQuery();
            }

            cmd.CommandText = $"INSERT INTO persons(id, first_name, second_name, last_name) VALUES ({item.id}, '{item.firstname}', '{item.middlename}', '{item.lastname}')";
            cmd.ExecuteNonQuery();

            if (item.email != string.Empty)
            {
                cmd.CommandText = $"INSERT INTO emails(email, person_id) VALUES ('{item.email}', (select id from persons where id = '{item.id}'))";
                cmd.ExecuteNonQuery();
            }

            if (item.phone != string.Empty)
            {
                cmd.CommandText = $"INSERT INTO phones(phone_number, person_id) VALUES ('{item.phone}', (select id from persons where id = '{item.id}'))";
                cmd.ExecuteNonQuery();
            }

            cmd.CommandText = $"INSERT INTO teachers(id, class_room_id, person_id, teaching_degree_id) VALUES ({item.id}, (select id from class_rooms ORDER BY id DESC LIMIT 1), {item.id}, (select id from teaching_degrees where id = {item.at_id}))";
            cmd.ExecuteNonQuery();
        }
        #endregion

        #region Coccectors
        foreach (var teacher in unparsed)
        {
            if (teacher.degree_description != "не выбран")
            {
                cmd.CommandText = $"INSERT INTO teachers_academic_degrees(teacher_id, academic_degree_id) VALUES ((select id from teachers where id = {teacher.id}), (select id from academic_degrees where name = '{teacher.degree_description}'))";
                cmd.ExecuteNonQuery();
            }

            foreach (var work in teacher.works)
            {
                cmd.CommandText = $"INSERT INTO teachers_departments(teacher_id, department_id) VALUES ((select id from teachers where id = {teacher.id}), (select id from departments where name = '{work.depname}'))";
                cmd.ExecuteNonQuery();

                cmd.CommandText = $"INSERT INTO teachers_positions(teacher_id, position_id) VALUES ((select id from teachers where id = {teacher.id}), (select id from positions where ((name = '{work.post}') and (department_id = (select id from departments where name = '{work.depname}'))) ))";
                cmd.ExecuteNonQuery();
            }
        }
        #endregion

        conn.Close();
    }
}
