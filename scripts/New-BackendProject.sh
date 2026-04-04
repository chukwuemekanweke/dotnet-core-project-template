#!/usr/bin/env bash

set -euo pipefail

organization_abbreviation=""
client_name=""
client_project_name=""
output_directory=""

while [[ $# -gt 0 ]]; do
  case "$1" in
    --organization-abbreviation)
      organization_abbreviation="${2:-}"
      shift 2
      ;;
    --client-name)
      client_name="${2:-}"
      shift 2
      ;;
    --client-project-name)
      client_project_name="${2:-}"
      shift 2
      ;;
    --output-directory)
      output_directory="${2:-}"
      shift 2
      ;;
    -h|--help)
      cat <<'EOF'
Usage:
  ./scripts/New-BackendProject.sh [options]

Options:
  --organization-abbreviation VALUE  Organization abbreviation, max 3 characters. Defaults to CN.
  --client-name VALUE                Client name segment.
  --client-project-name VALUE        Client project name segment.
  --output-directory VALUE           Output directory. Defaults to ./{Org}.{Client}.{Project}
  -h, --help                         Show this help text.
EOF
      exit 0
      ;;
    *)
      echo "Unknown argument: $1" >&2
      exit 1
      ;;
  esac
done

read_template_value() {
  local prompt="$1"
  local default_value="${2:-}"
  local value=""

  while true; do
    if [[ -n "$default_value" ]]; then
      read -r -p "$prompt [$default_value]: " value
    else
      read -r -p "$prompt: " value
    fi

    value="${value#"${value%%[![:space:]]*}"}"
    value="${value%"${value##*[![:space:]]}"}"

    if [[ -n "$value" ]]; then
      printf '%s\n' "$value"
      return
    fi

    if [[ -n "$default_value" ]]; then
      printf '%s\n' "$default_value"
      return
    fi
  done
}

convert_to_organization_abbreviation() {
  local value="$1"
  local normalized

  normalized="$(printf '%s' "$value" | tr -cd '[:alnum:]')"

  if [[ -z "$normalized" ]]; then
    echo "Organization abbreviation must contain letters or digits." >&2
    exit 1
  fi

  if [[ ${#normalized} -gt 3 ]]; then
    echo "Organization abbreviation cannot be longer than 3 characters." >&2
    exit 1
  fi

  printf '%s\n' "${normalized^^}"
}

convert_to_name_segment() {
  local value="$1"
  local normalized=""
  local token=""
  local character=""
  local index=0
  local capitalized_token=""

  while (( index < ${#value} )); do
    character="${value:index:1}"

    if [[ "$character" =~ [[:alnum:]] ]]; then
      token+="$character"
    else
      if [[ -n "$token" ]]; then
        if [[ ${#token} -eq 1 ]]; then
          normalized+="${token^^}"
        else
          capitalized_token="${token:0:1}"
          capitalized_token="${capitalized_token^^}${token:1}"
          normalized+="$capitalized_token"
        fi

        token=""
      fi
    fi

    ((index += 1))
  done

  if [[ -n "$token" ]]; then
    if [[ ${#token} -eq 1 ]]; then
      normalized+="${token^^}"
    else
      capitalized_token="${token:0:1}"
      capitalized_token="${capitalized_token^^}${token:1}"
      normalized+="$capitalized_token"
    fi
  fi

  if [[ -z "$normalized" ]]; then
    echo "Name values must contain letters or digits." >&2
    exit 1
  fi

  printf '%s\n' "$normalized"
}

if ! command -v dotnet >/dev/null 2>&1; then
  echo "The .NET SDK is required and 'dotnet' was not found on PATH." >&2
  exit 1
fi

script_root="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
template_root="$(cd "$script_root/.." && pwd)"

if [[ -z "${DOTNET_CLI_HOME:-}" ]]; then
  export DOTNET_CLI_HOME="$template_root/.dotnet"
fi

if [[ -z "${DOTNET_SKIP_FIRST_TIME_EXPERIENCE:-}" ]]; then
  export DOTNET_SKIP_FIRST_TIME_EXPERIENCE="1"
fi

if [[ -z "$organization_abbreviation" ]]; then
  organization_abbreviation="$(read_template_value "Organization abbreviation" "CN")"
fi

if [[ -z "$client_name" ]]; then
  client_name="$(read_template_value "Client name")"
fi

if [[ -z "$client_project_name" ]]; then
  client_project_name="$(read_template_value "Client project name")"
fi

organization_segment="$(convert_to_organization_abbreviation "$organization_abbreviation")"
client_segment="$(convert_to_name_segment "$client_name")"
project_segment="$(convert_to_name_segment "$client_project_name")"
root_name="$organization_segment.$client_segment.$project_segment"

if [[ -z "$output_directory" ]]; then
  resolved_output_directory="$PWD/$root_name"
else
  if [[ "$output_directory" = /* ]]; then
    resolved_output_directory="$output_directory"
  else
    resolved_output_directory="$PWD/$output_directory"
  fi
fi

resolved_output_directory="$(cd "$(dirname "$resolved_output_directory")" && pwd)/$(basename "$resolved_output_directory")"

if [[ -e "$resolved_output_directory" ]]; then
  echo "Output directory already exists: $resolved_output_directory" >&2
  exit 1
fi

printf '\n'
printf 'Template root name: %s\n' "$root_name"
printf 'Output directory:   %s\n' "$resolved_output_directory"
printf '\n'

printf 'Installing template from %s\n' "$template_root"
dotnet new install "$template_root" --force

printf '\n'
printf 'Creating project...\n'
dotnet new backend-template \
  --organizationAbbreviation "$organization_segment" \
  --clientName "$client_segment" \
  --clientProjectName "$project_segment" \
  --output "$resolved_output_directory"

printf '\n'
printf 'Created %s at %s\n' "$root_name" "$resolved_output_directory"
