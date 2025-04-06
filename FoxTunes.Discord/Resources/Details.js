(function () {
    if (!file) {
        return version();
    }
    var parts = [];
    if (tag.disccount != 1 && tag.disc) {
        parts.push(tag.disc);
    }
    if (tag.track) {
        parts.push(zeropad2(tag.track, tag.trackcount, 2));
    }
    if (tag.artist) {
        parts.push(tag.artist);
    }
    if (tag.album) {
        parts.push(tag.album);
    }
    if (tag.title) {
        parts.push(tag.title);
    }
    else {
        parts.push(filename(file));
    }
    if (tag.performer && tag.performer != tag.artist) {
        parts.push(tag.performer);
    }
    return parts.join(" - ");
})()