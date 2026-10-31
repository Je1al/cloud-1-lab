#!/bin/zsh
# Starts the lab on macOS and installs the required SDK when Homebrew is available.
set -e

if ! command -v brew >/dev/null 2>&1; then
  print "Homebrew is required to install .NET 8 automatically."
  print "Install Homebrew from https://brew.sh, then run this script again."
  exit 1
fi

if ! brew list --versions dotnet@8 >/dev/null 2>&1; then
  print "Installing .NET 8 SDK..."
  brew install dotnet@8
fi

dotnet_prefix="$(brew --prefix dotnet@8)"
export DOTNET_ROOT="$dotnet_prefix/libexec"
export PATH="$dotnet_prefix/bin:$PATH"

print "Using SDK: $(dotnet --version)"
dotnet run --project RRC.Runner -- --seed 42 --trace 0
