// ============================================
// Changelog Generator for GitHub Actions
// ============================================

// Dependencies
const fs = require("fs");
const yaml = require("js-yaml");
const axios = require("axios");

// Enhanced logging function
function log(message, level = "INFO") {
  const timestamp = new Date().toISOString();
  console.log(`[${timestamp}] [${level}] ${message}`);
}

// Debug: Log environment variables (masking tokens)
log("=== CHANGELOG SCRIPT STARTED ===");
log(`Script directory: ${__dirname}`);
log(`Current working directory: ${process.cwd()}`);

log("Environment variables:");
log(`- PR_NUMBER: ${process.env.PR_NUMBER}`);
log(`- GITHUB_REPOSITORY: ${process.env.GITHUB_REPOSITORY}`);
log(`- CHANGELOG_DIR: ${process.env.CHANGELOG_DIR}`);
log(`- GH_PAT exists: ${!!process.env.GH_PAT ? "YES (masked)" : "NO"}`);

// Use GitHub token if available for API calls
if (process.env.GH_PAT) {
  log("Setting up axios with GitHub Personal Access Token authentication");
  axios.defaults.headers.common["Authorization"] = `Bearer ${process.env.GH_PAT}`;
  axios.defaults.headers.common["Accept"] = "application/vnd.github.v3+json";
  axios.defaults.headers.common["User-Agent"] = "Changelog-Generator";
}

// Regex patterns for parsing changelog entries
const HeaderRegex = /^\s*(?::cl:|🆑) *([a-z0-9_\- ]+)?\s+/im;
const EntryRegex = /^ *[*-]? *(add|remove|tweak|fix): *([^\n\r]+)\r?$/img;
const CommentRegex = /<!--.*?-->/gs;

// Main function
async function main() {
  try {
    log(`Processing PR #${process.env.PR_NUMBER}`);

    // Step 1: Fetch PR details from GitHub API
    log(`Fetching PR details from GitHub API...`);
    const apiUrl = `https://api.github.com/repos/${process.env.GITHUB_REPOSITORY}/pulls/${process.env.PR_NUMBER}`;
    log(`API URL: ${apiUrl}`);

    const prResponse = await axios.get(apiUrl);
    const { merged_at, body, user, title, number } = prResponse.data;

    log(`PR Details:`);
    log(`- Title: ${title}`);
    log(`- Author: ${user.login}`);
    log(`- Merged at: ${merged_at}`);
    log(`- Body length: ${body ? body.length : 0} characters`);

    // Step 2: Check if PR was merged
    if (!merged_at) {
      log("PR was not merged, skipping changelog generation", "WARN");
      return;
    }

    // Step 3: Remove HTML comments from PR body
    const commentlessBody = body ? body.replace(CommentRegex, '') : '';
    log(`Body after removing comments: ${commentlessBody.length} characters`);

    // Step 4: Parse changelog header
    const headerMatch = HeaderRegex.exec(commentlessBody);
    if (!headerMatch) {
      log("No changelog header found (no :cl: or 🆑), skipping", "WARN");
      log(`Body preview (first 500 chars): ${commentlessBody.substring(0, 500)}...`);
      return;
    }

    let author = headerMatch[1];
    if (!author) {
      log("No author specified in changelog header, using PR author", "INFO");
      author = user.login;
    }

    log(`Changelog author: ${author}`);

    // Step 5: Parse changelog entries
    const entries = getChanges(commentlessBody);

    if (!entries || entries.length === 0) {
      log("No valid changelog entries found, skipping", "WARN");
      return;
    }

    log(`Found ${entries.length} changelog entries:`);
    entries.forEach((entry, index) => {
      log(`  ${index + 1}. ${entry.type}: ${entry.message}`);
    });

    // Step 6: Format timestamp
    let time = merged_at;
    if (time) {
      // Convert ISO format to match expected format
      time = time.replace("z", ".0000000+00:00").replace("Z", ".0000000+00:00");
      log(`Formatted time: ${time}`);
    }

    // Step 7: Get next available ID
    const nextId = getHighestCLNumber() + 1;
    log(`Next changelog ID: ${nextId}`);

    // Step 8: Construct changelog entry
    const entry = {
      author: author,
      changes: entries,
      id: nextId,
      time: time,
      url: `https://github.com/${process.env.GITHUB_REPOSITORY}/pull/${process.env.PR_NUMBER}`
    };

    log(`Constructed changelog entry: ${JSON.stringify(entry, null, 2)}`);

    // Step 9: Write changelog to file
    writeChangelog(entry);

    log(`Successfully updated changelog with entry #${nextId}`, "SUCCESS");

  } catch (error) {
    log(`ERROR in main function: ${error.message}`, "ERROR");
    log(`Stack trace: ${error.stack}`, "ERROR");
    if (error.response) {
      log(`API Response status: ${error.response.status}`, "ERROR");
      log(`API Response data: ${JSON.stringify(error.response.data)}`, "ERROR");
    }
    throw error; // Re-throw to fail the workflow
  }
}

// Helper function: Parse changelog entries from PR body
function getChanges(body) {
  log("Parsing changelog entries from PR body...");

  const matches = [];
  const entries = [];

  // Find all matches using regex
  let match;
  while ((match = EntryRegex.exec(body)) !== null) {
    matches.push([match[1], match[2]]);
    log(`Found raw entry: ${match[1]}: ${match[2]}`);
  }

  if (matches.length === 0) {
    log("No changelog entries matched the regex pattern", "WARN");
    return entries;
  }

  log(`Found ${matches.length} raw changelog entries`);

  // Process each match
  matches.forEach((rawEntry, index) => {
    const [rawType, message] = rawEntry;
    let type;

    // Normalize change type
    switch (rawType.toLowerCase()) {
      case "add":
        type = "Add";
        break;
      case "remove":
        type = "Remove";
        break;
      case "tweak":
        type = "Tweak";
        break;
      case "fix":
        type = "Fix";
        break;
      default:
        log(`Unknown change type: ${rawType}, skipping`, "WARN");
        return; // Skip this entry
    }

    if (type && message && message.trim()) {
      entries.push({
        type: type,
        message: message.trim(),
      });
      log(`Added entry ${index + 1}: ${type}: ${message.trim()}`);
    }
  });

  log(`Processed ${entries.length} valid changelog entries`);
  return entries;
}

// Helper function: Get highest changelog ID from file
function getHighestCLNumber() {
  const changelogPath = process.env.CHANGELOG_DIR;
  log(`Reading changelog file: ${changelogPath}`);

  try {
    // Check if file exists
    if (!fs.existsSync(changelogPath)) {
      log("Changelog file does not exist, starting from ID 0", "WARN");
      return 0;
    }

    // Read and parse YAML file
    const fileContent = fs.readFileSync(changelogPath, "utf8");
    log(`File size: ${fileContent.length} bytes`);

    const data = yaml.load(fileContent);
    if (!data || !data.Entries) {
      log("No entries found in changelog file, starting from ID 0", "INFO");
      return 0;
    }

    const entries = Array.from(data.Entries);
    log(`Found ${entries.length} existing entries in changelog`);

    // Get all IDs
    const ids = entries.map(entry => entry.id || 0);
    const maxId = Math.max(...ids, 0);

    log(`Current highest changelog ID: ${maxId}`);
    return maxId;

  } catch (error) {
    log(`ERROR reading changelog file: ${error.message}`, "ERROR");
    log(`Starting from ID 0 due to error`, "WARN");
    return 0;
  }
}

// Helper function: Write changelog entry to file
function writeChangelog(entry) {
  const changelogPath = process.env.CHANGELOG_DIR;
  log(`Writing to changelog file: ${changelogPath}`);

  try {
    let data = { Entries: [] };

    // Read existing changelog if it exists
    if (fs.existsSync(changelogPath)) {
      log("Reading existing changelog file...");
      const fileContent = fs.readFileSync(changelogPath, "utf8");
      data = yaml.load(fileContent) || { Entries: [] };
      log(`Loaded ${data.Entries.length} existing entries`);
    } else {
      log("Creating new changelog file", "INFO");
    }

    // Add new entry
    data.Entries.push(entry);
    log(`Added new entry, total entries: ${data.Entries.length}`);

    // Prepare YAML content
    const yamlContent = yaml.dump(data, {
      indent: 2,
      lineWidth: -1, // No line wrapping
      sortKeys: false // Preserve key order
    });

    // Write to file
    log(`Writing ${yamlContent.length} bytes to file...`);
    fs.writeFileSync(changelogPath, yamlContent, "utf8");

    // Verify write
    const writtenContent = fs.readFileSync(changelogPath, "utf8");
    log(`Write verified: ${writtenContent.length} bytes written`);

    // Log the new entry
    log(`New changelog entry added:`);
    log(`  ID: ${entry.id}`);
    log(`  Author: ${entry.author}`);
    log(`  Time: ${entry.time}`);
    log(`  URL: ${entry.url}`);
    log(`  Changes: ${entry.changes.length}`);

  } catch (error) {
    log(`ERROR writing changelog file: ${error.message}`, "ERROR");
    throw error;
  }
}

// Execute main function
main()
  .then(() => {
    log("=== CHANGELOG SCRIPT COMPLETED SUCCESSFULLY ===");
  })
  .catch(error => {
    log(`=== CHANGELOG SCRIPT FAILED: ${error.message} ===`, "ERROR");
    process.exit(1);
  });
