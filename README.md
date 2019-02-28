# FormBuilder_115
This repo is to showcase issue https://github.com/Microsoft/BotBuilder-V3/issues/115

Bot Builder Version
v3.20.1

Describe the bug
I have a model with a FormBuilder with a field that asks a string that should contain a date, asks LUIS about the resolution of that date and if there are multiple resolutions it appends to ValidateResult Choices the possible answers for the user to pick. But this only works fine in the emulator. Skype and Teams were tested so far with error BadRequest on bubbling up to parent dialog.

To Reproduce
Steps to reproduce the behavior:

FormBuilder with field of type string expecting to have "30-01-2019" when form is complete
Validate delegation to custom logic
Send response to LUIS to check for prebuild entity of type datetimev2
LUIS gives back 2+ resolutions for dates in the format of "dd-MM-yyyy"
For each resolution create choice and append it to ValidateResult.Choices
Return ValidateResult
Test in emulator: works
Test in Skype/MSTeams: don't work
Expected behavior
Give the same options as the emulator renders

Screenshots
Emulator:


Skype:


Additional context
.Field(nameof(Date), validate: async (state, response) =>
{
    var result = new ValidateResult { IsValid = true, Value = response };

    var luisResult = await LuisLog.GetLuisResultAsync(this.EmailAddress, response as string);

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
                                Description = new DescribeAttribute($"{item["value"].ToString()} ({ConversationHelpers.GetDayOfWeek(item["value"].ToString(), "yyyy-MM-dd")})", null, null, null, null),
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
