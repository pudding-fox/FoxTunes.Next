function version() {
    //The actual version could be returned.
    //return Publication.Product + " " + Publication.Version;
    return "Fox Tunes";
}

function timestamp(value) {

    if (!value) {
        return value;
    }

    var s = parseInt((value / 1000) % 60);
    var m = parseInt((value / (1000 * 60)) % 60);
    var h = parseInt((value / (1000 * 60 * 60)) % 24);

    var parts = [];

    if (h > 0) {
        if (h < 10) {
            h = "0" + h;
        }
        parts.push(h);
    }

    if (m < 10) {
        m = "0" + m;
    }
    parts.push(m);
    if (s < 10) {
        s = "0" + s;
    }
    parts.push(s);

    return parts.join(":");
}

function zeropad(value, width) {
    value = value + "";
    return value.length >= width ? value : new Array(width - value.length + 1).join("0") + value;
}

function zeropad2(value, max, width) {
    if (max) {
        max = max + "";
        width = max.length;
    }
    return zeropad(value, width);
}

function ucfirst(value) {
    if (!value) {
        return value;
    }
    var words = value.split(" ");
    for (var a = 0; a < words.length; a++) {
        words[a] = words[a].charAt(0).toUpperCase() + words[a].slice(1);
    }
    return words.join(" ");
}

function directoryname(value) {
    if (!value) {
        return value;
    }
    var parts = value.split('\\');
    if (parts.length > 1) {
        return parts[parts.length - 2];
    }
    else {
        return value;
    }
}

function filename(value) {
    if (!value) {
        return value;
    }
    var parts = value.split('\\');
    return parts[parts.length - 1].replace(/\.[^/.]+$/, '');
}

function extension(value) {
    if (!value) {
        return value;
    }
    {
        var parts = value.split("://");
        if (parts.length > 1) {
            return parts[0];
        }
    }
    {
        var parts = value.split(".");
        return parts[parts.length - 1];
    }
}

function content(value) {
    if (!value || !value.mime || !value.data) {
        return value;
    }
    if (value.mime == "application/json") {
        return JSON.parse(value.data);
    }
    return value.data;
}

function bitrate(sampleRate, depth, channels) {
    sampleRate = parseInt(sampleRate ? sampleRate : "44100");
    depth = parseInt(depth ? depth : "16");
    channels = parseInt(channels ? channels  : "2");
    return parseInt((sampleRate * depth * channels) / 1000) + " kbps";
}

function samplerate(sampleRate) {
    return (sampleRate ? sampleRate : 44100) + " Hz";
}

function channeldescription(channels) {
    switch (parseInt(channels ? channels : 2))
    {
        case 1:
            return "mono";
        case 2:
            return "stereo";
        case 4:
            return "quad";
        case 6:
            return "5.1";
        case 8:
            return "7.1";
    }
    return channels + " channels";
}
