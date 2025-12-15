import argparse

from micasa import status


def main():
    parser = argparse.ArgumentParser(
        description='Micasa - A Python CLI tool',
        prog='micasa'
    )

    subparsers = parser.add_subparsers(dest='command', help='Available commands')

    # Add 'status' subcommand
    status_parser = subparsers.add_parser('status', help='Show status')
    status_parser.add_argument('package', nargs='?', help='Optional package name to check')
    status_parser.add_argument('-v', '--verbose', action='store_true', help='Verbose output')

    # Add 'install' subcommand
    install_parser = subparsers.add_parser('install', help='Install something')
    install_parser.add_argument('package', nargs='?', help='Optional package name to install')
    install_parser.add_argument('-v', '--verbose', action='store_true', help='Verbose output')

    args = parser.parse_args()

    if args.command == 'status':
        status.run(args)
    elif args.command == 'install':
        print("Running install...")
    else:
        parser.print_help()
