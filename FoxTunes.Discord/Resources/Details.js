(function () {
    if (!file) {
        return version();
    }
    var parts = [];
    if (tag.artist) {
        parts.push(tag.artist);
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