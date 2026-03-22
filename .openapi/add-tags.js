const fs = require('fs');

const args = process.argv.slice(2);

const inputPath = args[0];
const outputPath = args[1];

// Load JSON from file
fs.readFile(inputPath, 'utf8', (err, data) => {
    if (err) {
        throw err;
    }
    const jsonData = JSON.parse(data);
    const paths = jsonData.paths;
    const pathKeys = Object.keys(paths)

    for (let i = 0; i < pathKeys.length; i++) {
        const key = pathKeys[i];
        const path = paths[key];
        const verbs = Object.keys(path)
        let tag = '';

        if (key.startsWith('/keys')) {
            tag = 'Keys';
        } else if (key.startsWith('/rng')) {
            tag = 'RNG';
        } else if (key.startsWith('/secrets')) {
            tag = 'Secrets';
        } else {
            tag = 'Unknown';
        }

        for (const verb of verbs) {
            if (Object.hasOwn(path[verb], 'tags')) {
                path[verb].tags.unshift(tag)
            } else {
                path[verb].tags = [tag];
            }
        }
    }

    // Save to a different file
    fs.writeFile(outputPath, JSON.stringify(jsonData, null, 2), (err) => {
        if (err) {
            throw err;
        }
        console.log(`Data saved to ${outputPath}`);
    });
});
