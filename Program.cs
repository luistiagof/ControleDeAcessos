using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace ControleDeAcessos
{
    public class Log
    {
        public DateTime DtAcesso { get; set; }
        public Usuario Usuario { get; set; }
        public bool TipoAcesso { get; set; }

        public override string ToString()
        {
            string status = TipoAcesso ? "AUTORIZADO" : "NEGADO";
            return $"[{DtAcesso:dd/MM/yyyy HH:mm:ss}] Usuário: {Usuario.Nome} (ID: {Usuario.Id}) - Status: {status}";
        }
    }

    public class Ambiente
    {
        public int Id { get; set; }
        public string Nome { get; set; }
        public Queue<Log> Logs { get; set; } = new Queue<Log>();

        public void RegistrarLog(Log log)
        {
            if (Logs.Count >= 100)
            {
                Logs.Dequeue();
            }
            Logs.Enqueue(log);
        }
    }

    public class Usuario
    {
        public int Id { get; set; }
        public string Nome { get; set; }
        public List<Ambiente> Ambientes { get; set; } = new List<Ambiente>();

        public bool ConcederPermissao(Ambiente ambiente)
        {
            if (!Ambientes.Any(a => a.Id == ambiente.Id))
            {
                Ambientes.Add(ambiente);
                return true;
            }
            return false;
        }

        public bool RevogarPermissao(Ambiente ambiente)
        {
            var amb = Ambientes.FirstOrDefault(a => a.Id == ambiente.Id);
            if (amb != null)
            {
                Ambientes.Remove(amb);
                return true;
            }
            return false;
        }
    }

    public class Cadastro
    {
        public List<Usuario> Usuarios { get; set; } = new List<Usuario>();
        public List<Ambiente> Ambientes { get; set; } = new List<Ambiente>();

        private const string ArquivoDados = "dados_sistema.json";

        public void AdicionarUsuario(Usuario usuario)
        {
            Usuarios.Add(usuario);
        }

        public bool RemoverUsuario(Usuario usuario)
        {
            if (usuario.Ambientes.Count > 0)
            {
                return false;
            }
            return Usuarios.Remove(usuario);
        }

        public Usuario PesquisarUsuario(Usuario usuarioBusca)
        {
            return Usuarios.FirstOrDefault(u => u.Id == usuarioBusca.Id || u.Nome.Equals(usuarioBusca.Nome, StringComparison.OrdinalIgnoreCase));
        }

        public void AdicionarAmbiente(Ambiente ambiente)
        {
            Ambientes.Add(ambiente);
        }

        public bool RemoverAmbiente(Ambiente ambiente)
        {
            foreach (var user in Usuarios)
            {
                user.RevogarPermissao(ambiente);
            }
            return Ambientes.Remove(ambiente);
        }

        public Ambiente PesquisarAmbiente(Ambiente ambienteBusca)
        {
            return Ambientes.FirstOrDefault(a => a.Id == ambienteBusca.Id || a.Nome.Equals(ambienteBusca.Nome, StringComparison.OrdinalIgnoreCase));
        }

        public void Upload()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                var dados = new { Usuarios, Ambientes };
                string jsonString = JsonSerializer.Serialize(dados, options);
                File.WriteAllText(ArquivoDados, jsonString);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro: {ex.Message}");
            }
        }

        public void Download()
        {
            try
            {
                if (File.Exists(ArquivoDados))
                {
                    string jsonString = File.ReadAllText(ArquivoDados);
                    using (JsonDocument doc = JsonDocument.Parse(jsonString))
                    {
                        var root = doc.RootElement;
                        var jsonUsuarios = root.GetProperty("Usuarios");
                        var jsonAmbientes = root.GetProperty("Ambientes");

                        Ambientes = JsonSerializer.Deserialize<List<Ambiente>>(jsonAmbientes.GetRawText()) ?? new List<Ambiente>();
                        Usuarios = JsonSerializer.Deserialize<List<Usuario>>(jsonUsuarios.GetRawText()) ?? new List<Usuario>();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro: {ex.Message}");
            }
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Cadastro cadastro = new Cadastro();
            cadastro.Download();

            int opcao = -1;
            while (opcao != 0)
            {
                Console.Clear();
                Console.WriteLine("========================================");
                Console.WriteLine("       PROJETO CONTROLE DE ACESSOS      ");
                Console.WriteLine("========================================");
                Console.WriteLine("0. Sair");
                Console.WriteLine("1. Cadastrar ambiente");
                Console.WriteLine("2. Consultar ambiente");
                Console.WriteLine("3. Excluir ambiente");
                Console.WriteLine("4. Cadastrar usuário");
                Console.WriteLine("5. Consultar usuário");
                Console.WriteLine("6. Excluir usuário");
                Console.WriteLine("7. Conceder permissão de acesso");
                Console.WriteLine("8. Revogar permissão de acesso");
                Console.WriteLine("9. Registrar acesso");
                Console.WriteLine("10. Consultar logs de acesso");
                Console.WriteLine("========================================");
                Console.Write("Escolha uma opção: ");

                if (!int.TryParse(Console.ReadLine(), out opcao))
                {
                    opcao = -1;
                }

                switch (opcao)
                {
                    case 0:
                        cadastro.Upload();
                        break;
                    case 1:
                        CadastrarAmbiente(cadastro);
                        break;
                    case 2:
                        ConsultarAmbiente(cadastro);
                        break;
                    case 3:
                        ExcluirAmbiente(cadastro);
                        break;
                    case 4:
                        CadastrarUsuario(cadastro);
                        break;
                    case 5:
                        ConsultarUsuario(cadastro);
                        break;
                    case 6:
                        ExcluirUsuario(cadastro);
                        break;
                    case 7:
                        ConcederPermissao(cadastro);
                        break;
                    case 8:
                        RevogarPermissao(cadastro);
                        break;
                    case 9:
                        RegistrarAcesso(cadastro);
                        break;
                    case 10:
                        ConsultarLogs(cadastro);
                        break;
                    default:
                        Console.WriteLine("\nOpção inválida!");
                        Console.ReadKey();
                        break;
                }
            }
        }

        static void CadastrarAmbiente(Cadastro cadastro)
        {
            Console.Clear();
            Console.Write("ID do Ambiente: ");
            if (int.TryParse(Console.ReadLine(), out int id))
            {
                Console.Write("Nome do Ambiente: ");
                string nome = Console.ReadLine();
                cadastro.AdicionarAmbiente(new Ambiente { Id = id, Nome = nome });
                Console.WriteLine("\nSucesso!");
            }
            Console.ReadKey();
        }

        static void ConsultarAmbiente(Cadastro cadastro)
        {
            Console.Clear();
            foreach (var amb in cadastro.Ambientes)
            {
                Console.WriteLine($"ID: {amb.Id} | Nome: {amb.Nome} | Logs: {amb.Logs.Count}");
            }
            Console.ReadKey();
        }

        static void ExcluirAmbiente(Cadastro cadastro)
        {
            Console.Clear();
            Console.Write("ID do ambiente: ");
            if (int.TryParse(Console.ReadLine(), out int id))
            {
                var amb = cadastro.Ambientes.FirstOrDefault(a => a.Id == id);
                if (amb != null)
                {
                    cadastro.RemoverAmbiente(amb);
                    Console.WriteLine("\nExcluído!");
                }
            }
            Console.ReadKey();
        }

        static void CadastrarUsuario(Cadastro cadastro)
        {
            Console.Clear();
            Console.Write("ID do Usuário: ");
            if (int.TryParse(Console.ReadLine(), out int id))
            {
                Console.Write("Nome do Usuário: ");
                string nome = Console.ReadLine();
                cadastro.AdicionarUsuario(new Usuario { Id = id, Nome = nome });
                Console.WriteLine("\nSucesso!");
            }
            Console.ReadKey();
        }

        static void ConsultarUsuario(Cadastro cadastro)
        {
            Console.Clear();
            foreach (var user in cadastro.Usuarios)
            {
                string permissoes = string.Join(", ", user.Ambientes.Select(a => a.Nome));
                Console.WriteLine($"ID: {user.Id} | Nome: {user.Nome} | Ambientes: [{permissoes}]");
            }
            Console.ReadKey();
        }

        static void ExcluirUsuario(Cadastro cadastro)
        {
            Console.Clear();
            Console.Write("ID do usuário: ");
            if (int.TryParse(Console.ReadLine(), out int id))
            {
                var user = cadastro.Usuarios.FirstOrDefault(u => u.Id == id);
                if (user != null)
                {
                    if (cadastro.RemoverUsuario(user))
                        Console.WriteLine("\nExcluído!");
                    else
                        Console.WriteLine("\nErro: O usuário possui permissões ativas.");
                }
            }
            Console.ReadKey();
        }

        static void ConcederPermissao(Cadastro cadastro)
        {
            Console.Clear();
            Console.Write("ID do Usuário: ");
            int idUser = int.Parse(Console.ReadLine() ?? "0");
            Console.Write("ID do Ambiente: ");
            int idAmb = int.Parse(Console.ReadLine() ?? "0");

            var user = cadastro.Usuarios.FirstOrDefault(u => u.Id == idUser);
            var amb = cadastro.Ambientes.FirstOrDefault(a => a.Id == idAmb);

            if (user != null && amb != null)
            {
                user.ConcederPermissao(amb);
                Console.WriteLine("\nFeito!");
            }
            Console.ReadKey();
        }

        static void RevogarPermissao(Cadastro cadastro)
        {
            Console.Clear();
            Console.Write("ID do Usuário: ");
            int idUser = int.Parse(Console.ReadLine() ?? "0");
            Console.Write("ID do Ambiente: ");
            int idAmb = int.Parse(Console.ReadLine() ?? "0");

            var user = cadastro.Usuarios.FirstOrDefault(u => u.Id == idUser);
            var amb = cadastro.Ambientes.FirstOrDefault(a => a.Id == idAmb);

            if (user != null && amb != null)
            {
                user.RevogarPermissao(amb);
                Console.WriteLine("\nFeito!");
            }
            Console.ReadKey();
        }

        static void RegistrarAcesso(Cadastro cadastro)
        {
            Console.Clear();
            Console.Write("ID do Usuário: ");
            int idUser = int.Parse(Console.ReadLine() ?? "0");
            Console.Write("ID do Ambiente: ");
            int idAmb = int.Parse(Console.ReadLine() ?? "0");

            var user = cadastro.Usuarios.FirstOrDefault(u => u.Id == idUser);
            var amb = cadastro.Ambientes.FirstOrDefault(a => a.Id == idAmb);

            if (user != null && amb != null)
            {
                bool temPermissao = user.Ambientes.Any(a => a.Id == amb.Id);
                Log novoLog = new Log
                {
                    DtAcesso = DateTime.Now,
                    Usuario = user,
                    TipoAcesso = temPermissao
                };
                amb.RegistrarLog(novoLog);
                Console.WriteLine(temPermissao ? "\nAutorizado!" : "\nNegado!");
            }
            Console.ReadKey();
        }

        static void ConsultarLogs(Cadastro cadastro)
        {
            Console.Clear();
            Console.Write("ID do Ambiente: ");
            int idAmb = int.Parse(Console.ReadLine() ?? "0");

            var amb = cadastro.Ambientes.FirstOrDefault(a => a.Id == idAmb);
            if (amb != null)
            {
                Console.WriteLine("1. Todos\n2. Autorizados\n3. Negados");
                string filtro = Console.ReadLine();

                var logs = amb.Logs.AsEnumerable();
                if (filtro == "2") logs = logs.Where(l => l.TipoAcesso);
                else if (filtro == "3") logs = logs.Where(l => !l.TipoAcesso);

                foreach (var log in logs)
                {
                    Console.WriteLine(log);
                }
            }
            Console.ReadKey();
        }
    }
}
