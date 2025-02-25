(function () {
    var parts = [];
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