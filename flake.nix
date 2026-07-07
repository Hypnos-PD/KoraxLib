{
  description = "KoraxLib dev shell";
  inputs.nixpkgs.url = "github:NixOS/nixpkgs/nixos-unstable";
  outputs = { self, nixpkgs }:
  let
    system = "x86_64-linux";
    pkgs = nixpkgs.legacyPackages.${system};
  in {
    devShells.${system}.default = pkgs.mkShell {
      buildInputs = [
        pkgs.actionlint
        pkgs.dotnet-sdk_9
        pkgs.nodejs_22
        pkgs.playwright-driver
      ];

      shellHook = ''
        export DOTNET_ROOT=${pkgs.dotnet-sdk_9}/share/dotnet
        export PLAYWRIGHT_BROWSERS_PATH=${pkgs.playwright-driver.browsers}
        export PLAYWRIGHT_SKIP_BROWSER_DOWNLOAD=1
        echo "已进入 KoraxLib 开发环境"
      '';
    };
  };
}
