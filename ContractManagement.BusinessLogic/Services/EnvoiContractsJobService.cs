using Microsoft.Extensions.Hosting;
using Quartz;
using MongoDB.Driver;
using ContractManagement.BusinessLogic.Services.Interfaces;
using MongoDB.Bson;
using Quartz.Impl;
using iTextSharp.text.pdf;

public class EnvoiContractsJobService : IJob, IHostedService
{
    private readonly IContractService _contraService;
    private readonly IMongoDatabase _database;
    private readonly ISendPendingEmails _sendPendingEmails;
    private readonly IMailService _mailService;

    public EnvoiContractsJobService(IContractService contraService, IMongoDatabase database, ISendPendingEmails sendPendingEmails, IMailService mailService)
    {
        _contraService = contraService;
        _database = database;
        _sendPendingEmails = sendPendingEmails;
        _mailService = mailService;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        byte[] pdfData = await GeneratePdf();

        await _mailService.SendEmailWithAttachmentAsync("lauriatzahouly@gmail.com", "Document Avis-Echeance", pdfData, "Avis-Echeance.pdf", CancellationToken.None);
        //// Logique pour l'exécution du travail planifié via Quartz.NET
        //// Récupération de la liste des contrats à échéance sur 7 jours
        //var contracts = await _contraService.GetContractsByDueDateAsync(30, context.CancellationToken);

        //// Création du contenu de l'e-mail avec un tableau HTML
        //var emailContent = "<h2>Liste des contrats à échéance :</h2>";
        //emailContent += "<table style='border-collapse: collapse;'>";
        //emailContent += "<tr><th style='border: 1px solid black; padding: 8px;'>ID Contrat</th> <th style='border: 1px solid black; padding: 8px;'>Numéro de police</th> <th style='border: 1px solid black; padding: 8px;'>Date de création</th><th style='border: 1px solid black; padding: 8px;'>Échéance</th></tr>";

        //foreach (var contract in contracts)
        //{
        //    emailContent += $"<tr><td style='border: 1px solid black; padding: 8px;'>{contract.Id}</td><td style='border: 1px solid black; padding: 8px;'>{contract.PolicyNumber}</td><td style='border: 1px solid black; padding: 8px;'>{contract.Created}</td><td style='border: 1px solid black; padding: 8px;'>{contract.DueDate}</td></tr>";
        //}

        //emailContent += "</table>";

        //// Enregistrement Jobs dans MongoDB cette méthode doit être dans mon program.cs 
        //var email = new BsonDocument
        //{
        //    { "recipient_email", "maud.kirlin@ethereal.email" },
        //    { "subject", "Liste des contrats à échéance" },
        //    { "body", emailContent },
        //    { "scheduled_time", DateTime.UtcNow },
        //    { "status", "En attente d'envoi" }
        //};
        //await _database.GetCollection<BsonDocument>("Jobs").InsertOneAsync(email);
        //await _sendPendingEmails.SendPendingEmail(context.CancellationToken);
    }

    public  async Task StartAsync(CancellationToken cancellationToken)
    {
        // Démarrer le planificateur Quartz.NET ici
        // Par exemple :
        var schedulerFactory = new StdSchedulerFactory();
        var scheduler = await schedulerFactory.GetScheduler();
        await scheduler.Start();

        // Planifier le travail EnvoiContractsJobService avec Quartz.NET
        var job = JobBuilder.Create<EnvoiContractsJobService>().Build();
        var trigger = TriggerBuilder.Create()
            .WithIdentity("EnvoiContractsTrigger", "default")
            .WithSimpleSchedule(x => x
                .WithIntervalInSeconds(60) // Par exemple, toutes les 60 secondes
                .RepeatForever())
            .Build();

        await scheduler.ScheduleJob(job, trigger);

        await Task.CompletedTask;

    }
    public  async Task StopAsync(CancellationToken cancellationToken)
    {
        var schedulerFactory = new StdSchedulerFactory();
        var scheduler = await schedulerFactory.GetScheduler();
        await scheduler.Shutdown();
    }

    private async Task<byte[]> GeneratePdf()
    {
        string cheminFichierPDF = @"C:\Users\INFI-2\Desktop\Project-Infi\Asp.net core\Finafrica-Project\ContractManagement.WebApi\Ressources\Avis-Echeance.pdf";
        string[] nomsDesChamps = { "fullName", "address", "policyNumber", "dateEcheance", "amountPrime", "netPrime", "accessories", "tax", "fga", "totalPrime", "article", "dateReglement", "date", "insurer" };
        string[] valeursDesChamps = {
                "John Doe",
                "123 Main St, City, Country",
                "POL987654",
                "31/12/2024",
                "€600",
                "€580",
                "accessoires",
                "€25",
                "€25",
                "€15",
                "€615",
                "Description de l'article",
                "25/11/2023",
                "Spvie assurance"
                };

        byte[] pdfData;

        using (MemoryStream memoryStream = new MemoryStream())
        {
            using (PdfReader reader = new PdfReader(cheminFichierPDF))
            {
                using (PdfStamper stamper = new PdfStamper(reader, memoryStream))
                {
                    AcroFields form = stamper.AcroFields;

                    for (int i = 0; i < nomsDesChamps.Length; i++)
                    {
                        // Remplir les champs avec les valeurs correspondantes
                        form.SetField(nomsDesChamps[i], valeursDesChamps[i]);
                    }

                    stamper.Close();
                }
            }

            pdfData = memoryStream.ToArray();
        }

        Console.WriteLine("Champs du document PDF remplis avec succès !");
        return pdfData;
    }
}
