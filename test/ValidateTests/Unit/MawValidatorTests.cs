﻿
namespace FormatValidatorTests.Unit
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net.Mail;
    using FormatValidator;
    using FormatValidator.Validators;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Newtonsoft.Json;

    [TestClass]
    public class MawValidatorTests
    {
        [TestMethod]
        public void Validator_ReturnsAValidator()
        {
            string[] files = new string[] { @"data\045-Interest-sallen@wish.org.csv" };

            foreach (var file in files)
            {
                List<RowValidationError> rowErrors = new List<RowValidationError>();
                List<ColumnValidationError> colErrors = new List<ColumnValidationError>();

                string[] parts = Path.GetFileName(file).Replace(".csv", "").Split('-');

                if (parts.Length == 3)
                {
                    string chid = parts[0];
                    string function = parts[1];
                    string email = parts[2];
                    string[] header = new string[] { };

                    try
                    {
                        string JSON = System.IO.File.ReadAllText(@"data\configuration\maw-" + function.ToLower() + "-config.json");

                        // Add the chapter id to the json string
                        var config = JsonConvert.DeserializeObject<IDictionary<string, object>>(JSON);
                        config.Add("chapterId", chid);
                        JSON = JsonConvert.SerializeObject(config);

                        Validator validator = Validator.FromJson(JSON);
                        FileSourceReader reader = new FileSourceReader(file);

                        colErrors = new List<ColumnValidationError>(validator.ValidateCols(reader));
                        rowErrors = new List<RowValidationError>(validator.Validate(reader));

                        header = validator.Header;

                        string emailFrom = "no-reply@apps.wish.org";
                        string emailTo = email;
                        string subject = String.Format("LO Import {0} File Validation", function);
                        string content = "";

                        MailMessage message = new MailMessage();
                        message.From = new MailAddress(emailFrom);
                        message.To.Add(emailTo);
                        message.Subject = subject;

                        if (rowErrors.Count == 0)
                        {
                            content = String.Format("The attached file {0}-{1}-{2}.csv has been validated successfully with no errors.  " +
                                "Please proceed with the import into LO and remember to check the count of records " +
                                "imported in LO with the count of records in the file.", chid, function, email);

                            message.Body = content;
                            message.Attachments.Add(new Attachment(file));
                        }
                        else
                        {
                            string errorLog = GenerateStandardsErrorLog(rowErrors, header);
                            string exceptionFileName = String.Format(@"data\{0}-{1}-{2}-Standards.csv", chid, function, email);

                            File.WriteAllText(exceptionFileName, errorLog);

                            content = String.Format("The attached file {0}-{1}-{2}.csv has been validated and there are errors.  " +
                                "The details of the errors that need to be corrected are attached.  " +
                                "If you need help resolving these errors, please raise a ticket at help.wish.org for the integrated fundraising team.  " +
                                "Once the errors are corrected, please drop the updated file in the validation location.", chid, function, email);

                            message.Body = content;
                            message.Attachments.Add(new Attachment(file));
                            message.Attachments.Add(new Attachment(exceptionFileName));
                        }

                        //SmtpClient smtp = new SmtpClient("wish-org.mail.protection.outlook.com");
                        SmtpClient smtp = new SmtpClient("192.168.100.34");
                        smtp.Send(message);
                    }
                    catch (Exception ex)
                    {

                    }
                }
            }
        }

        /// <summary>
        /// The generate error log.
        /// </summary>
        /// <param name="errors">The errors.</param>
        /// <returns>The result.</returns>
        private string GenerateStandardsErrorLog(List<RowValidationError> errors, string[] header)
        {
            string content = "COLUMN,Row,Rule Code\n";

            for (var i = 0; i < errors.Count; i++)
            {
                string row = errors[i].Row.ToString();
                foreach (var error in errors[0].Errors)
                {
                    string colName = header[error.Column - 1];
                    content += String.Format("{0},{1},{2}\n", colName, row, error.Message);
                }
            }

            return content;
        }
    }
}