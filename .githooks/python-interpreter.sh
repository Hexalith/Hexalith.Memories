# Source from hooks after changing to the repository root.
for py in python3 python; do
  if command -v "$py" >/dev/null 2>&1 &&
    "$py" -c 'import sys; sys.exit(sys.version_info < (3, 9))' >/dev/null 2>&1; then
    return 0
  fi
done

echo "Git hooks require Python 3.9 or newer. Install it and make python3 or python available on PATH." >&2
exit 1
