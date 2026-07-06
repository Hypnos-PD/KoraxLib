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
        pkgs.dotnet-sdk_9
      ];

      shellHook = ''
        export DOTNET_ROOT=${pkgs.dotnet-sdk_9}/share/dotnet
        echo "已进入 KoraxLib 开发环境"
      '';
    };
  };
}