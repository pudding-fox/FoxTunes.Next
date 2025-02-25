(function () {
    var parts = [];
    if (tag.disccount != 1 && tag.disc) {
        parts.push(tag.disc);
    }
    if (tag.track) {
        parts.push(zeropad2(tag.track, tag.trackcount, 2));
    }
    if (tag.title) {
        parts.push(tag.title);
    }
    else {
        parts.push(filename(file));
    }
    return parts.join(" - ");
})()