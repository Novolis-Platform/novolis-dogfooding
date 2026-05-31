using Novolis.Audio.Voice.Design;
using Novolis.Audio.Voice.Platform;
using Novolis.Dogfooding.Voice;

namespace Novolis.Dogfooding.Voice.Unit;

public class KokoroVoiceArchetypeCatalogTests
{
    [Test]
    public async Task ExcitableFemale_uses_isabella_kokoro_model()
    {
        var archetype = KokoroVoiceArchetypeCatalog.ExcitableFemale;
        await Assert.That(archetype.Profile.Id).IsEqualTo("excitable_female");
        await Assert.That(archetype.Model.Id).IsEqualTo("kokoro:bf_isabella");
        await Assert.That(archetype.SpeakingRate).IsEqualTo(1.21f);
    }

    [Test]
    public async Task ExcitableFemaleDraft_exports_kokoro_archetype_snippet()
    {
        var draft = KokoroVoiceArchetypeCatalog.ExcitableFemaleDraft();
        await Assert.That(draft.Backend).IsEqualTo(VoiceSynthesizerBackend.KokoroOnnx);

        var code = DogfoodingVoiceCodeEmitter.Emit(draft, DogfoodingVoiceCodeTemplate.AtcDeliveryStatic);
        await Assert.That(code).Contains("ExcitableFemaleDelivery");

        code = VoicePresetCodeEmitter.Emit(draft, VoicePresetCodeTemplate.ArchetypeCatalogEntry);
        await Assert.That(code).Contains("kokoro:bf_isabella");
        await Assert.That(code).Contains("UseKokoro");
    }
}
