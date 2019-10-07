
var microsoftTeams;
var tenantId;

// Set up the tab and stuff.
microsoftTeams.initialize();

// Get selected teams.
function getSelectedTeams() {
    var teams = [];
    $("#eventTeam :selected").each(function () {
        teams.push({
            Id: $(this).val()
        });
    });
    return teams;
}

// submit the data to task module opener.
function submitForm(action, eventId) {
    var eventInfo = {
        Id: eventId,
        Type: $('#eventType :selected').val(),
        Title: $('#title').val(),
        Message: $('#message').val(),
        Date: $('#eventDate').val(),
        ImageUrl: $(".carousel-item.active > img").attr("src"),
        Teams: getSelectedTeams(),
        TimeZoneId: $('#timezonelist :selected').val(),
    };
    if (isValidDate($('#eventDate').val())) {
        $("#invalidDate").hide();
    } else {
        $("#invalidDate").show();
        return false;
    }

    eventData = {
        Action: action,
        EventInfo: eventInfo
    };

    microsoftTeams.tasks.submitTask(eventData);
    return true;
}

function isValidDate(date) {
    var separator = date.match(/[.\/\-\s].*?/),
        parts = date.split(/\W+/);
    if (!separator || !parts || parts.length < 3) {
        return false;
    }
    if (isNaN(Date.parse(date))) {
        return false;
    }

    return true;
}

function closeTaskModule() {
    microsoftTeams.tasks.submitTask();
}