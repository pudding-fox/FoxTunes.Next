(function () {
    var parts1 = [];
    var parts2 = [];
    if (tag.disccount != 1 && tag.disc) {
        parts1.push(tag.disc);
    }
    if (tag.track) {
        parts1.push(zeropad2(tag.track, tag.trackcount, 2));
    }
    if (tag.title) {
        parts1.push(tag.title);
    }
    else {
        parts1.push(filename(file));
    }
    if (tag.performer) {
        parts2.push(tag.performer);
    }
    else if (tag.artist) {
        parts2.push(tag.artist);
    }
    return parts1.join(" - ") + "\n" + parts2.join(" - ") 
})()