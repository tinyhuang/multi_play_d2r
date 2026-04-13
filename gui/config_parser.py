"""Parse and write .bat configuration files for D2R Multi-Play Launcher."""

import os
import re

# Pattern to match: set VAR=VALUE  or  set "VAR=VALUE"
_SET_PATTERN = re.compile(
    r'^set\s+"?([A-Za-z_][A-Za-z0-9_]*)=(.*?)"?\s*$',
    re.IGNORECASE,
)


def parse_bat(filepath):
    """Read a .bat file and return (lines, variables).

    lines: list of original lines (for write-back preserving structure).
    variables: dict {VAR_NAME: value_string}.
    """
    variables = {}
    lines = []
    if not os.path.isfile(filepath):
        return lines, variables
    with open(filepath, "r", encoding="utf-8", errors="replace") as f:
        lines = f.readlines()
    for line in lines:
        stripped = line.strip()
        m = _SET_PATTERN.match(stripped)
        if m:
            var_name = m.group(1)
            raw_val = m.group(2)
            # Strip surrounding quotes from value
            if raw_val.startswith('"') and raw_val.endswith('"'):
                raw_val = raw_val[1:-1]
            variables[var_name] = raw_val
    return lines, variables


def write_bat(filepath, lines, variables):
    """Write modified variables back into the bat file lines.

    Updates existing 'set' lines in-place. Variables not found in existing
    lines are appended at the end.
    """
    written_keys = set()
    new_lines = []
    for line in lines:
        stripped = line.strip()
        m = _SET_PATTERN.match(stripped)
        if m:
            var_name = m.group(1)
            if var_name in variables:
                val = variables[var_name]
                # Determine quoting style: use set "VAR=VALUE" for safety
                new_lines.append(f'set "{var_name}={val}"\n')
                written_keys.add(var_name)
            else:
                new_lines.append(line)
        else:
            new_lines.append(line)

    # Append any new variables not in original file
    remaining = set(variables.keys()) - written_keys
    if remaining:
        new_lines.append("\n")
        for key in sorted(remaining):
            new_lines.append(f'set "{key}={variables[key]}"\n')

    with open(filepath, "w", encoding="utf-8", newline="\r\n") as f:
        f.writelines(new_lines)


# ---------------------------------------------------------------------------
# High-level helpers for application-specific config
# ---------------------------------------------------------------------------

def load_base_settings(config_dir):
    """Load base_settings.bat and return parsed dict."""
    path = os.path.join(config_dir, "base_settings.bat")
    lines, variables = parse_bat(path)
    return {
        "_path": path,
        "_lines": lines,
        "secs": variables.get("secs", "8"),
        "addres": variables.get("addres", "").strip('"'),
        "diablo": variables.get("diablo", "").strip('"'),
        "workdir": variables.get("workdir", ""),
        "MONITOR_IDS": variables.get("MONITOR_IDS", "1"),
        "DEFAULT_WIN_W": variables.get("DEFAULT_WIN_W", "1280"),
        "DEFAULT_WIN_H": variables.get("DEFAULT_WIN_H", "720"),
        "MIN_WIN_W": variables.get("MIN_WIN_W", "800"),
        "MIN_WIN_H": variables.get("MIN_WIN_H", "600"),
        "PRIMARY_WIN_W": variables.get("PRIMARY_WIN_W", "1920"),
        "PRIMARY_WIN_H": variables.get("PRIMARY_WIN_H", "1080"),
        "_monitors": _parse_monitors(variables),
        "_raw": variables,
    }


def _parse_monitors(variables):
    """Extract per-monitor settings from variables dict."""
    monitors = {}
    ids_str = variables.get("MONITOR_IDS", "1")
    for mon_id in ids_str.split():
        mon_id = mon_id.strip()
        if not mon_id:
            continue
        monitors[mon_id] = {
            "W": int(variables.get(f"MON_{mon_id}_W", "1920")),
            "H": int(variables.get(f"MON_{mon_id}_H", "1080")),
            "SCALE": int(variables.get(f"MON_{mon_id}_SCALE", "100")),
            "X": int(variables.get(f"MON_{mon_id}_X", "0")),
            "Y": int(variables.get(f"MON_{mon_id}_Y", "0")),
            "TASKBAR": int(variables.get(f"MON_{mon_id}_TASKBAR", "0")),
        }
    return monitors


def load_accounts(config_dir):
    """Load accounts_secrets.bat and return list of 8 account dicts."""
    path = os.path.join(config_dir, "accounts_secrets.bat")
    lines, variables = parse_bat(path)
    accounts = []
    for i in range(1, 9):
        prefix = f"ACCOUNT_{i}_"
        accounts.append({
            "id": i,
            "ENABLE": variables.get(f"{prefix}ENABLE", "0"),
            "USER": variables.get(f"{prefix}USER", ""),
            "PASS": variables.get(f"{prefix}PASS", ""),
            "MOD": variables.get(f"{prefix}MOD", ""),
            "OPTIONS": variables.get(f"{prefix}OPTIONS", ""),
            "MONITOR": variables.get(f"{prefix}MONITOR", ""),
            "DIABLO": variables.get(f"{prefix}DIABLO", ""),
            "PRIMARY": variables.get(f"{prefix}PRIMARY", "0"),
            "WIN_X": variables.get(f"{prefix}WIN_X", ""),
            "WIN_Y": variables.get(f"{prefix}WIN_Y", ""),
        })
    return {"_path": path, "_lines": lines, "_raw": variables, "accounts": accounts}


def save_base_settings(config_dir, settings, monitors):
    """Write base_settings.bat with updated values."""
    path = os.path.join(config_dir, "base_settings.bat")
    if os.path.isfile(path):
        lines, existing = parse_bat(path)
    else:
        lines, existing = [], {}

    # Update variables
    variables = dict(existing)
    variables["secs"] = str(settings.get("secs", "8"))
    variables["addres"] = f'"{settings.get("addres", "")}"'
    variables["diablo"] = f'"{settings.get("diablo", "")}"'
    variables["workdir"] = settings.get("workdir", "")
    variables["DEFAULT_WIN_W"] = str(settings.get("DEFAULT_WIN_W", "1280"))
    variables["DEFAULT_WIN_H"] = str(settings.get("DEFAULT_WIN_H", "720"))
    variables["MIN_WIN_W"] = str(settings.get("MIN_WIN_W", "800"))
    variables["MIN_WIN_H"] = str(settings.get("MIN_WIN_H", "600"))
    variables["PRIMARY_WIN_W"] = str(settings.get("PRIMARY_WIN_W", "1920"))
    variables["PRIMARY_WIN_H"] = str(settings.get("PRIMARY_WIN_H", "1080"))

    # Monitor IDs
    mon_ids = sorted(monitors.keys(), key=lambda x: int(x))
    variables["MONITOR_IDS"] = " ".join(mon_ids)

    # Per-monitor settings
    for mid, mon in monitors.items():
        variables[f"MON_{mid}_W"] = str(mon.get("W", 1920))
        variables[f"MON_{mid}_H"] = str(mon.get("H", 1080))
        variables[f"MON_{mid}_SCALE"] = str(mon.get("SCALE", 100))
        variables[f"MON_{mid}_X"] = str(mon.get("X", 0))
        variables[f"MON_{mid}_Y"] = str(mon.get("Y", 0))
        variables[f"MON_{mid}_TASKBAR"] = str(mon.get("TASKBAR", 0))

    write_bat(path, lines, variables)


def save_accounts(config_dir, accounts):
    """Write accounts_secrets.bat with updated values."""
    path = os.path.join(config_dir, "accounts_secrets.bat")
    if os.path.isfile(path):
        lines, existing = parse_bat(path)
    else:
        lines, existing = [], {}

    variables = dict(existing)
    for acct in accounts:
        i = acct["id"]
        prefix = f"ACCOUNT_{i}_"
        variables[f"{prefix}ENABLE"] = str(acct.get("ENABLE", "0"))
        variables[f"{prefix}USER"] = acct.get("USER", "")
        variables[f"{prefix}PASS"] = acct.get("PASS", "")
        variables[f"{prefix}MOD"] = acct.get("MOD", "")
        variables[f"{prefix}OPTIONS"] = acct.get("OPTIONS", "")
        variables[f"{prefix}MONITOR"] = acct.get("MONITOR", "")
        variables[f"{prefix}DIABLO"] = acct.get("DIABLO", "")
        variables[f"{prefix}PRIMARY"] = str(acct.get("PRIMARY", "0"))
        variables[f"{prefix}WIN_X"] = acct.get("WIN_X", "")
        variables[f"{prefix}WIN_Y"] = acct.get("WIN_Y", "")

    write_bat(path, lines, variables)
