# TusCli

A cli tool for interacting with a Tus enabled server.

## Install

```bash
dotnet tool install -g TusCli
```

## Usage

`tus --help` output:

```bash
A cli tool for interacting with a Tus enabled server.

Usage: tus [arguments] [options]

Arguments:
  file                      File to upload
  address                   The endpoint of the Tus server

Options:
  -m|--metadata <METADATA>  Additional metadata to submit. Format: key1=value1,key2=value2
  -?|-h|--help              Show help information
```
