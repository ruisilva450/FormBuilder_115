using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.FormFlow;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FormBuilder_115.Models
{
    [Serializable]
    public class FormBuilderTestModel
    {
        public FormBuilderTestModel()
        {

        }

        [Prompt("Em que **data** estiveste nesta tarefa?")]
        public string Date { get; set; }

        public IForm<FormBuilderTestModel> BuildForm()
        {
            return new FormBuilder<FormBuilderTestModel>()
                .Field(nameof(Date), validate: async (state, response) =>
                {
                    var result = new ValidateResult { IsValid = true, Value = response };

                    var luisResult = await GetLuisResultAsync(response as string);

                    bool foundResult = false;
                    bool parcialResult = false;

                    foreach (var entity in luisResult.Entities)
                    {
                        if (!foundResult && !parcialResult)
                        {
                            switch (entity.Type)
                            {
                                case "builtin.datetimeV2.date":
                                    if (JArray.Parse(JsonConvert.SerializeObject(entity.Resolution.Values.First())).Count > 1)
                                    {
                                        result.Choices = new List<Choice>();
                                        foreach (var item in JArray.Parse(JsonConvert.SerializeObject(entity.Resolution.Values.First())))
                                        {
                                            result.Choices = result.Choices.Append(new Choice()
                                            {
                                                Value = item["value"].ToString(),
                                                Description = new DescribeAttribute($"{item["value"].ToString()} ({GetDayOfWeek(item["value"].ToString(), "yyyy-MM-dd")})", null, null, null, null),
                                                Terms = new TermsAttribute(item["value"].ToString())
                                            });
                                        }

                                        foundResult = false;
                                        parcialResult = true;
                                    }
                                    else
                                    {
                                        result.Value = JArray.Parse(JsonConvert.SerializeObject(entity.Resolution.Values.First())).First()["value"].ToObject<DateTime>().ToString("dd-MM-yyyy");
                                        parcialResult = true;
                                        foundResult = true;
                                    }
                                    break;
                                case "builtin.number":
                                    var number = int.Parse(entity.Resolution.Values.First().ToString());
                                    var date = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, number).ToString("dd-MM-yyyy");
                                    result.Value = date;
                                    parcialResult = true;
                                    foundResult = true;
                                    break;
                                default:
                                    break;
                            }
                        }
                    }

                    result.IsValid = foundResult;
                    return result;
                })
                .Message((state) =>
                {
                    var result = $"Date: {state.Date}";

                    return Task.FromResult(new PromptAttribute(result));
                })
                .OnCompletion(ProcessValidation)
                .Build();
        }

        public string GetDayOfWeek(string date, string format = "dd-MM-yyyy")
        {
            if (DateTime.TryParseExact(date, format, CultureInfo.InvariantCulture,
            DateTimeStyles.None, out DateTime dateObj))
                return GetDayOfWeek(dateObj);
            else
                return string.Empty;
        }

        public string GetDayOfWeek(DateTime date)
        {
            var addMessage = date.Date == DateTime.UtcNow.Date ? " (Today)" : string.Empty;
            switch (date.DayOfWeek)
            {
                case DayOfWeek.Sunday:
                    return $"Sunday{addMessage}";
                case DayOfWeek.Monday:
                    return $"Monday{addMessage}";
                case DayOfWeek.Tuesday:
                    return $"Tuesday{addMessage}";
                case DayOfWeek.Wednesday:
                    return $"Wednesday{addMessage}";
                case DayOfWeek.Thursday:
                    return $"Thursday{addMessage}";
                case DayOfWeek.Friday:
                    return $"Friday{addMessage}";
                case DayOfWeek.Saturday:
                    return $"Saturday{addMessage}";
                default:
                    return addMessage;
            }
        }

        private async Task<LuisResult> GetLuisResultAsync(string input)
        {
            LuisService luisService = new LuisService(model: new LuisModelAttribute(
                                          ConfigurationManager.AppSettings["LUISApplicationID"],
                                          ConfigurationManager.AppSettings["LUISAuthoringKey"],
                                          domain: ConfigurationManager.AppSettings["LuisAPIHostName"]));

            return await luisService.QueryAsync(input, new CancellationToken());
        }

        private async Task ProcessValidation(IDialogContext context, FormBuilderTestModel state)
        {
            if (DateTime.TryParseExact(state.Date, "dd-MM-yyyy", CultureInfo.InvariantCulture,
            DateTimeStyles.None, out DateTime dateObj))
            {
                await context.SayAsync($"ProcessValidation -> Date: {dateObj}");
            }
        }
    }
}