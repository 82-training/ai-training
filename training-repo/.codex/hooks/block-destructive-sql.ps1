$inputPayload = [Console]::In.ReadToEnd()
if ($inputPayload -match 'DROP\s+TABLE|TRUNCATE') {
    [Console]::Error.WriteLine('Action denied: destructive SQL is not allowed by this project hook.')
    exit 2
}

exit 0
